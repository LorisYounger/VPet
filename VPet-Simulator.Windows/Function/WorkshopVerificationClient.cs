using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 创意工坊上传登记和本地内容一致性校验。
    /// </summary>
    internal static class WorkshopVerificationClient
    {
        private const ulong IndividualSteamIdBase = 76561197960265728UL;

        private static readonly HttpClient Client = new()
        {
            //BaseAddress = new Uri("http://localhost:5830/")
            BaseAddress = new Uri("https://wsv.exlb.net/")
        };

        /// <summary>
        /// 从 MOD 的 info.lps、目录名和文件内容生成校验元数据。
        /// </summary>
        internal static WorkshopMetadata ReadMetadata(DirectoryInfo directory, ulong? workshopId = null, ulong? steamId = null)
        {
            string infoPath = Path.Combine(directory.FullName, "info.lps");
            var document = new LpsDocument(File.ReadAllText(infoPath));
            string modId = document.FindLine("vupmod")?.Info?.Trim() ?? directory.Name;
            long authorId = document.FindLine("authorid")?.InfoToInt64 ?? 0;
            ulong itemId = workshopId ?? ReadWorkshopId(document, directory);

            return new WorkshopMetadata
            {
                WorkshopId = checked((long)itemId),
                SteamId = checked((long)(steamId ?? ToFullSteamId(authorId))),
                ModId = modId,
                ModNameEN = ReadLocalizedName(document, modId, "en"),
                ModNameHans = ReadLocalizedName(document, modId, "zh-Hans"),
                FileHash = CalculateDirectoryHash(directory),
                DllFiles = directory.EnumerateFiles("*.dll", SearchOption.AllDirectories)
                    .Select(file => Path.GetRelativePath(directory.FullName, file.FullName).Replace('\\', '/'))
                    .OrderBy(file => file, StringComparer.Ordinal)
                    .ToList()
            };
        }

        /// <summary>
        /// 向验证服务登记一次创意工坊上传或更新。
        /// </summary>
        internal static Task<WorkshopUploadResponse> RegisterUploadAsync(
            DirectoryInfo directory,
            ulong workshopId,
            ulong steamId,
            int checkKey,
            CancellationToken cancellationToken = default)
        {
            WorkshopMetadata metadata = ReadMetadata(directory, workshopId, steamId);
            var request = new UploadWorkshopRequest
            {
                CheckKey = checkKey,
                SteamId = metadata.SteamId,
                WorkshopId = metadata.WorkshopId,
                ModId = metadata.ModId,
                ModNameEN = metadata.ModNameEN,
                ModNameHans = metadata.ModNameHans,
                FileHash = metadata.FileHash,
                DllFiles = metadata.DllFiles
            };

            return PostAsync<WorkshopUploadResponse>("workshop/upload", request, cancellationToken);
        }

        /// <summary>
        /// 使用 Steam 提供的作品/作者 ID 与本地 MOD 内容向验证服务校验创意工坊项目。
        /// </summary>
        internal static Task<WorkshopVerifyResponse> VerifyAsync(
            DirectoryInfo directory,
            long workshopId,
            long steamId,
            CancellationToken cancellationToken = default)
        {
            WorkshopMetadata metadata = ReadMetadata(
                directory,
                workshopId > 0 ? checked((ulong)workshopId) : null,
                steamId > 0 ? ToFullSteamId(steamId) : null);
            var request = new VerifyWorkshopRequest
            {
                SteamId = metadata.SteamId,
                WorkshopId = metadata.WorkshopId,
                ModId = metadata.ModId,
                ModNameEN = metadata.ModNameEN,
                ModNameHans = metadata.ModNameHans,
                FileHash = metadata.FileHash,
                DllFiles = metadata.DllFiles
            };

            return PostAsync<WorkshopVerifyResponse>("workshop/verify", request, cancellationToken);
        }

        /// <summary>
        /// 获取适合显示给用户的本地化 MOD 名称。
        /// </summary>
        internal static string GetModDisplayName(DirectoryInfo directory)
        {
            try
            {
                var document = new LpsDocument(File.ReadAllText(Path.Combine(directory.FullName, "info.lps")));
                string name = document.FindLine("vupmod")?.Info?.Trim() ?? directory.Name;

                foreach (var line in document.FindAllLine("lang"))
                {
                    if (!string.Equals(line.Info?.Trim(), LocalizeCore.CurrentCulture, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string? localizedName = line.Find(name)?.Info?.Trim();
                    if (!string.IsNullOrWhiteSpace(localizedName))
                        return localizedName;
                }

                return name.Translate();
            }
            catch
            {
                return directory.Name;
            }
        }

        /// <summary>
        /// 判断 MOD 是否包含会被 CoreMOD 加载的代码插件。
        /// </summary>
        internal static bool HasCodePlugin(DirectoryInfo directory)
        {
            var pluginDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "plugin"));
            if (!pluginDirectory.Exists)
                return false;

            try
            {
                // CoreMOD 只会从 plugin 文件夹的当前层加载 DLL。
                return pluginDirectory.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly).Any();
            }
            catch
            {
                // 无法确认目录内容时保留校验，避免把读取异常误判成无代码插件。
                return true;
            }
        }

        /// <summary>
        /// 将校验结果转换为用户可读的错误信息；校验通过时返回 <see langword="null"/>。
        /// </summary>
        internal static string? GetVerificationErrorMessage(WorkshopVerifyResponse response)
        {
            if (response.Ok && response.Consistent && !response.Risk)
                return null;

            var errors = new List<string>();
            if (!string.IsNullOrWhiteSpace(response.Message))
                errors.Add(TranslateVerificationMessage(response.Message));

            if (response.Risk)
            {
                errors.Add(string.IsNullOrWhiteSpace(response.RiskType)
                    ? "检测到风险内容".Translate()
                    : TranslateVerificationMessage(response.RiskType));
            }

            WorkshopVerifyResponse.WorkshopVerifyDetailsResponse details = response.Details;
            if (!details.ModIdMatched)
                errors.Add("MOD 标识不一致".Translate());
            if (!details.ModNameENMatched)
                errors.Add("MOD 英文名称不一致".Translate());
            if (!details.ModNameHansMatched)
                errors.Add("MOD 简体中文名称不一致".Translate());
            if (details.FileHashMatched == false)
                errors.Add("MOD 文件内容不一致".Translate());
            if (!details.SteamIdMatched)
                errors.Add("Steam 作者不一致".Translate());

            if (errors.Count == 0)
                errors.Add("创意工坊校验未通过".Translate());

            return string.Join("\n", errors.Distinct());
        }

        private static string TranslateVerificationMessage(string message)
        {
            return message switch
            {
                "VerifyMismatchMessage" => "MOD 信息或文件与作者上传的版本不一致".Translate(),
                "DetectedVirusSample" => "检测到已确认的风险样本".Translate(),
                "LegacyVerifiedMessage" => "MOD 已通过历史兼容验证".Translate(),
                "VerifyPassedMessage" => "MOD 完整性验证通过".Translate(),
                "NonAuthorMessage" => "该创意工坊作品已绑定其他作者".Translate(),
                "NoDLLMOD" => "非代码插件MOD检测出代码".Translate(),
                _ => message.Translate()
            };
        }

        private static async Task<T> PostAsync<T>(string path, object request, CancellationToken cancellationToken)
            where T : class, new()
        {
            string lps = LPSConvert.SerializeObject(request, convertNoneLineAttribute: true).ToString();
            using var content = new StringContent(lps, Encoding.UTF8, "application/lps");
            using HttpResponseMessage response = await Client.PostAsync(path, content, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return LPSConvert.DeserializeObject<T>(
                new LPS(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)),
                convertNoneLineAttribute: true)!;
        }

        private static ulong ReadWorkshopId(LpsDocument document, DirectoryInfo directory)
        {
            // 订阅目录名由 Steam 分配，优先于可被 MOD 内容修改的 info.lps。
            if (ulong.TryParse(directory.Name, out ulong itemId) && itemId != 0)
                return itemId;

            string? configuredId = document.FindLine("itemid")?.Info;
            if (ulong.TryParse(configuredId, out itemId) && itemId != 0)
                return itemId;
            return 0;
        }

        private static ulong ToFullSteamId(long authorId)
        {
            if (authorId >= 76560000000000000L)
                return checked((ulong)authorId);
            if (authorId > 0 && authorId <= uint.MaxValue)
                return IndividualSteamIdBase + checked((uint)authorId);
            return 0;
        }

        private static string ReadLocalizedName(LpsDocument document, string modId, string culture)
        {
            foreach (var line in document.FindAllLine("lang"))
            {
                if (!string.Equals(line.Info?.Trim(), culture, StringComparison.OrdinalIgnoreCase))
                    continue;

                return line.Find(modId)?.Info?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        private static long CalculateDirectoryHash(DirectoryInfo directory)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] separator = [0];
            byte[] buffer = new byte[81920];

            foreach (FileInfo file in directory.EnumerateFiles("*.dll", SearchOption.AllDirectories)
                         .OrderBy(file => Path.GetRelativePath(directory.FullName, file.FullName), StringComparer.Ordinal))
            {
                string relativePath = Path.GetRelativePath(directory.FullName, file.FullName).Replace('\\', '/');
                hash.AppendData(Encoding.UTF8.GetBytes(relativePath));
                hash.AppendData(separator);

                using FileStream stream = file.OpenRead();
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    hash.AppendData(buffer, 0, read);

                hash.AppendData(separator);
            }

            byte[] digest = hash.GetHashAndReset();
            long value = BinaryPrimitives.ReadInt64BigEndian(digest.AsSpan(0, sizeof(long)));
            return value == 0 ? 1 : value;
        }
    }

    internal class WorkshopMetadata
    {
        public long SteamId { get; set; }
        public long WorkshopId { get; set; }
        public string ModId { get; set; } = string.Empty;
        public string ModNameEN { get; set; } = string.Empty;
        public string ModNameHans { get; set; } = string.Empty;
        public long FileHash { get; set; }
        public List<string> DllFiles { get; set; } = [];
    }

    /// <summary>
    /// 上传或更新创意工坊模组的请求。
    /// </summary>
    public sealed class UploadWorkshopRequest
    {
        /// <summary>
        /// 客户端校验标记，当前暂不参与校验。
        /// </summary>
        public int CheckKey { get; init; }

        /// <summary>
        /// Steam 作者标识。
        /// </summary>
        public long SteamId { get; init; }

        /// <summary>
        /// Steam 创意工坊项目标识。
        /// </summary>
        public long WorkshopId { get; init; }

        /// <summary>
        /// 唯一的模组标识。
        /// </summary>
        [Required, MaxLength(256)]
        public required string ModId { get; init; }

        /// <summary>
        /// 模组英文名。
        /// </summary>
        [MaxLength(256)]
        public string? ModNameEN { get; init; }

        /// <summary>
        /// 模组简体中文名。
        /// </summary>
        [MaxLength(256)]
        public string? ModNameHans { get; init; }

        /// <summary>
        /// 客户端上报的文件哈希值。
        /// </summary>
        public long FileHash { get; init; }

        /// <summary>
        /// 客户端上传的所有 DLL 文件名列表，用于服务端进行样本检测。
        /// </summary>
        public List<string> DllFiles { get; init; } = [];
    }

    /// <summary>
    /// 上传流程返回结果。
    /// </summary>
    public sealed class WorkshopUploadResponse
    {
        /// <summary>
        /// 表示操作是否成功。
        /// </summary>
        public bool Ok { get; init; }

        /// <summary>
        /// 表示调用方是否允许上传。
        /// </summary>
        public bool CanUpload { get; init; }

        /// <summary>
        /// 操作类型：新建或更新。
        /// </summary>
        public string Action { get; init; } = string.Empty;

        /// <summary>
        /// Steam 创意工坊项目标识。
        /// </summary>
        public long WorkshopId { get; init; }

        /// <summary>
        /// 上传成功后新建的会话标识。
        /// </summary>
        public long? SessionId { get; init; }

        /// <summary>
        /// 阻止上传的冲突项。
        /// </summary>
        public List<WorkshopConflictResponse> Conflicts { get; init; } = [];

        /// <summary>
        /// 创意工坊元数据冲突项。
        /// </summary>
        public sealed class WorkshopConflictResponse
        {
            /// <summary>
            /// 冲突字段类型。
            /// </summary>
            public required string Type { get; init; }

            /// <summary>
            /// 发生冲突的字段值。
            /// </summary>
            public required string Value { get; init; }

            /// <summary>
            /// 已使用该值的创意工坊项目标识。
            /// </summary>
            public long WorkshopId { get; init; }

            /// <summary>
            /// 占用该值的 Steam 作者标识。
            /// </summary>
            public long SteamId { get; init; }
        }

        /// <summary>
        /// 结果消息。
        /// </summary>
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>
    /// 根据已存储元数据校验创意工坊模组的请求。
    /// </summary>
    public sealed class VerifyWorkshopRequest
    {
        /// <summary>
        /// Steam 创意工坊项目标识。
        /// </summary>
        public long WorkshopId { get; init; }

        /// <summary>
        /// Steam 作者标识。
        /// </summary>
        public long SteamId { get; init; }

        /// <summary>
        /// 唯一的模组标识。
        /// </summary>
        [Required, MaxLength(256)]
        public required string ModId { get; init; }

        /// <summary>
        /// 模组英文名。
        /// </summary>
        [MaxLength(256)]
        public string? ModNameEN { get; init; }

        /// <summary>
        /// 模组简体中文名。
        /// </summary>
        [MaxLength(256)]
        public string? ModNameHans { get; init; }

        /// <summary>
        /// 客户端上报的文件哈希值。
        /// </summary>
        public long FileHash { get; init; }

        /// <summary>
        /// 客户端上传的所有 DLL 文件名列表，用于服务端进行样本检测。
        /// </summary>
        public List<string> DllFiles { get; init; } = [];
    }

    /// <summary>
    /// 校验流程返回结果。
    /// </summary>
    public sealed class WorkshopVerifyResponse
    {
        /// <summary>
        /// 创建供 LPS 反序列化使用的空结果。
        /// </summary>
        [SetsRequiredMembers]
        public WorkshopVerifyResponse()
        {
            Details = new WorkshopVerifyDetailsResponse();
        }

        /// <summary>
        /// 表示校验是否通过。
        /// </summary>
        public bool Ok { get; init; }

        /// <summary>
        /// 表示所有参与比对的字段是否一致。
        /// </summary>
        public bool Consistent { get; init; }

        /// <summary>
        /// 校验所使用的数据来源：session 或 item。
        /// </summary>
        public string CheckedBy { get; init; } = string.Empty;

        /// <summary>
        /// 表示是否检测到风险信号。
        /// </summary>
        public bool Risk { get; init; }

        /// <summary>
        /// 检测到风险时的风险类型。
        /// </summary>
        public string RiskType { get; init; } = string.Empty;

        /// <summary>
        /// 校验结果消息。
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// 逐字段校验详情。
        /// </summary>
        public required WorkshopVerifyDetailsResponse Details { get; init; }

        /// <summary>
        /// 逐字段校验结果详情。
        /// </summary>
        public sealed class WorkshopVerifyDetailsResponse
        {
            /// <summary>
            /// 表示 ModID 是否匹配。
            /// </summary>
            public bool ModIdMatched { get; init; }

            /// <summary>
            /// 表示模组英文名是否匹配。
            /// </summary>
            public bool ModNameENMatched { get; init; }

            /// <summary>
            /// 表示模组中文名是否匹配。
            /// </summary>
            public bool ModNameHansMatched { get; init; }

            /// <summary>
            /// 表示文件哈希是否匹配，Null 表示未参与校验。
            /// </summary>
            public bool? FileHashMatched { get; init; }

            /// <summary>
            /// 表示 SteamID 是否匹配。
            /// </summary>
            public bool SteamIdMatched { get; init; }
        }
    }
}
