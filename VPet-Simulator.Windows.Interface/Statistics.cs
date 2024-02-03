using LinePutScript;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 统计
    /// </summary>
    public class Statistics : IGetOBJ<SetObject>
    {
        public Statistics() { }
        public Statistics(IEnumerable<ISub> subs)
        {
            AddRange(subs);
        }
        public void AddRange(IEnumerable<ISub> subs)
        {
            foreach (var sub in subs)
            {
                Data.Add(sub.Name, sub.info);
            }
        }
        /// <summary>
        /// 统计变化通知事件
        /// </summary>
        /// <param name="sender">发送的统计(this)</param>
        /// <param name="name">变动的名称</param>
        /// <param name="value">变动的值</param>
        public delegate void StatisticChangedEventHandler(Statistics sender, string name, SetObject value);

        public event StatisticChangedEventHandler StatisticChanged;
        /// <summary>
        /// 统计数据字典
        /// </summary>
        public SortedDictionary<string, SetObject> Data = new SortedDictionary<string, SetObject>();

        #region IGetOBJ<SetObject>
        public DateTime this[gdat subName]
        {
            get => GetDateTime((string)subName);
            set => SetDateTime((string)subName, value);
        }
        public FInt64 this[gflt subName]
        {
            get => GetFloat((string)subName);
            set => SetFloat((string)subName, value);
        }
        public double this[gdbe subName]
        {
            get => GetDouble((string)subName);
            set => SetDouble((string)subName, value);
        }
        public long this[gi64 subName]
        {
            get => GetInt64((string)subName);
            set => SetInt64((string)subName, value);
        }
        public int this[gint subName]
        {
            get => GetInt((string)subName);
            set => SetInt((string)subName, value);
        }
        public bool this[gbol subName]
        {
            get => GetBool((string)subName);
            set => SetBool((string)subName, value);
        }
        public string this[gstr subName]
        {
            get => GetString((string)subName);
            set => SetString((string)subName, value);
        }
        public SetObject this[string subName] { get => Find(subName) ?? new SetObject(); set => Set(subName, value); }
        public SetObject Find(string subName)
        {
            if (Data.TryGetValue(subName, out SetObject value))
                return value;
            else
                return null;
        }
        public void Set(string subName, SetObject value)
        {
            StatisticChanged?.Invoke(this, subName, value);
            Data[subName] = value;
        }
        /// <summary>
        /// 输出统计数据
        /// </summary>
        public List<Sub> ToSubs()
        {
            List<Sub> subs = new List<Sub>();
            foreach (var item in Data)
            {
                subs.Add(new Sub(item.Key, item.Value));
            }
            return subs;
        }

        public bool GetBool(string subName) => Find(subName)?.GetBoolean() ?? false;

        public void SetBool(string subName, bool value) => Set(subName, value);

        public int GetInt(string subName, int defaultvalue = 0) => Find(subName)?.GetInteger() ?? defaultvalue;

        public void SetInt(string subName, int value) => Set(subName, value);

        public long GetInt64(string subName, long defaultvalue = 0) => Find(subName)?.GetInteger64() ?? defaultvalue;

        public void SetInt64(string subName, long value) => Set(subName, value);

        public FInt64 GetFloat(string subName, FInt64 defaultvalue = default) => Find(subName)?.GetFloat() ?? defaultvalue;

        public void SetFloat(string subName, FInt64 value) => Set(subName, new SetObject(value));

        public DateTime GetDateTime(string subName, DateTime defaultvalue = default) => Find(subName)?.GetDateTime() ?? defaultvalue;

        public void SetDateTime(string subName, DateTime value) => Set(subName, value);

        public string GetString(string subName, string defaultvalue = null) => Find(subName)?.GetString() ?? defaultvalue;

        public void SetString(string subName, string value) => Set(subName, value);

        public double GetDouble(string subName, double defaultvalue = 0) => Find(subName)?.GetDouble() ?? defaultvalue;

        public void SetDouble(string subName, double value) => Set(subName, value);
        #endregion
    }
}
