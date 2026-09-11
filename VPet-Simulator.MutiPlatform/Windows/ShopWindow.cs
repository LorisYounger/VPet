using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.MutiPlatform.Windows;

/// <summary>
/// 更好买 (商店 + 背包)
/// </summary>
/// 对应 Windows 版的 winBetterBuy 和 winInventory. 那边是两个窗口, 这里合成一个
/// 两页的 —— 玩家买完东西下一步多半就是看背包, 分成两个窗口只是多点一次。
///
/// 所有判定(赊账、私房钱、超模、自动购买)都走共享后端的 PurchaseRules /
/// FoodPricing, 这个窗口只管摆控件和收点击。
internal sealed class ShopWindow : VPetWindow
{
    private readonly PetWindow host;
    private readonly TextBlock money = new TextBlock();
    private readonly TextBox search = new TextBox { Watermark = LocalizeCore.Translate("搜索"), MinWidth = 160 };
    private readonly ComboBox sort = new ComboBox { MinWidth = 120 };
    private readonly TabControl tabs = new TabControl();
    private readonly PagedWrapPanel<FoodItem> shopGrid;
    private readonly PagedWrapPanel<StoreItem> bagGrid;
    private readonly NumericUpDown count = new NumericUpDown
    {
        Minimum = 1,
        Maximum = 99,
        Value = 1,
        MinWidth = 90,
        Increment = 1,
        FormatString = "0",
    };

    private FoodItem.FoodType? filterType;

    internal ShopWindow(PetWindow host)
    {
        this.host = host;
        Title = LocalizeCore.Translate("更好买");
        CanResize = true;
        SizeToContent = SizeToContent.Manual;
        Width = 720;
        Height = 560;

        shopGrid = new PagedWrapPanel<FoodItem>(BuildFoodCell) { PageSize = 12 };
        bagGrid = new PagedWrapPanel<StoreItem>(BuildItemCell) { PageSize = 12 };

        Body = BuildRoot();
        RefreshMoney();
        RefreshShop();
        RefreshBag();
        CheckLoan();
    }

    private Control BuildRoot()
    {
        var root = new DockPanel();

        // 顶部: 金钱 + 搜索 + 排序 + 数量
        var top = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 8),
        };
        money.VerticalAlignment = VerticalAlignment.Center;
        money.FontSize = 16;
        top.Children.Add(money);
        top.Children.Add(search);
        sort.ItemsSource = new[]
        {
            LocalizeCore.Translate("价格从低到高"),
            LocalizeCore.Translate("价格从高到低"),
            LocalizeCore.Translate("名字"),
        };
        sort.SelectedIndex = 0;
        sort.SelectionChanged += (_, _) => RefreshShop();
        top.Children.Add(sort);
        var countLabel = new TextBlock
        {
            Text = LocalizeCore.Translate("数量"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        top.Children.Add(countLabel);
        top.Children.Add(count);
        search.TextChanged += (_, _) => RefreshShop();
        DockPanel.SetDock(top, Dock.Top);
        root.Children.Add(top);

        // 分类
        var types = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 0, 0, 8),
        };
        types.Children.Add(TypeButton(LocalizeCore.Translate("全部"), null));
        foreach (var (type, name) in ShopCategories)
            types.Children.Add(TypeButton(LocalizeCore.Translate(name), type));
        DockPanel.SetDock(types, Dock.Top);
        root.Children.Add(types);

        tabs.Items.Add(new TabItem { Header = LocalizeCore.Translate("商店"), Content = shopGrid });
        tabs.Items.Add(new TabItem { Header = LocalizeCore.Translate("背包"), Content = bagGrid });
        root.Children.Add(tabs);
        return root;
    }

    /// <summary>
    /// 分类与顺序, 与 Windows 版"投喂"菜单一致
    /// </summary>
    private static readonly (FoodItem.FoodType Type, string Name)[] ShopCategories =
    {
        (FoodItem.FoodType.Meal, "正餐"),
        (FoodItem.FoodType.Snack, "零食"),
        (FoodItem.FoodType.Drink, "饮料"),
        (FoodItem.FoodType.Functional, "功能性"),
        (FoodItem.FoodType.Drug, "药品"),
        (FoodItem.FoodType.Gift, "礼品"),
        (FoodItem.FoodType.Food, "食物"),
    };

    private Button TypeButton(string text, FoodItem.FoodType? type)
        => SecondaryButton(text, () =>
        {
            filterType = type;
            RefreshShop();
        });

    // ---- 商店 ----

    private void RefreshShop()
    {
        var all = host.HostResources.Foods.AsEnumerable();
        if (filterType != null)
            all = all.Where(x => x.Type == filterType);
        var keyword = search.Text;
        if (!string.IsNullOrWhiteSpace(keyword))
            all = all.Where(x => x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.NameTrans.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        all = sort.SelectedIndex switch
        {
            1 => all.OrderByDescending(x => x.Price),
            2 => all.OrderBy(x => x.NameTrans, StringComparer.CurrentCulture),
            _ => all.OrderBy(x => x.Price),
        };
        shopGrid.SetSource(all.ToList());
    }

    private Control BuildFoodCell(FoodItem food)
        //格子长什么样在 ShopCell 里, 渲染探针画的就是它 —— 两处各写一份的话
        //探针就只是在检查探针自己
        => ShopCell.Build(
            food.NameTrans,
            $"${food.Price:f1}",
            //超模提示: 与自动购买用的是同一个判定
            food.IsOverLoad() ? LocalizeCore.Translate("超模") : null,
            ShopCell.Describe(FoodValues(food)),
            host.HostImages.Get(food.ImagePath),
            LocalizeCore.Translate("购买"),
            () => Buy(food));

    /// <summary>
    /// 食物的数值描述
    /// </summary>
    /// 与 Windows 版一样只列不为零的项
    private static IEnumerable<(string, double)> FoodValues(FoodItem food)
    {
        yield return (LocalizeCore.Translate("经验"), food.Exp);
        yield return (LocalizeCore.Translate("饱腹"), food.StrengthFood);
        yield return (LocalizeCore.Translate("口渴"), food.StrengthDrink);
        yield return (LocalizeCore.Translate("体力"), food.Strength);
        yield return (LocalizeCore.Translate("心情"), food.Feeling);
        yield return (LocalizeCore.Translate("健康"), food.Health);
        yield return (LocalizeCore.Translate("好感"), food.Likability);
    }

    private async void Buy(FoodItem food)
    {
        var save = host.HostGameSave.GameSave;
        int times = (int)(count.Value ?? 1);
        for (int i = 0; i < times; i++)
        {
            //赊账规则在共享后端里: 便宜东西买不起也能买
            if (!PurchaseRules.CanAfford(food.Price, food.Exp, save.Money))
            {
                await DialogService.ShowAsync(
                    LocalizeCore.Translate("您没有足够金钱来购买 {0}\n您需要 {1:f2} 金钱来购买\n您当前 {2:f2} 拥有金钱",
                        food.NameTrans, food.Price, save.Money),
                    LocalizeCore.Translate("金钱不足"), this);
                break;
            }
            if (host.HostGameSave.HashCheck && food.IsOverLoad())
            {
                var ok = await DialogService.ConfirmAsync(
                    LocalizeCore.Translate("当前食物/物品属性超模,是否继续使用?\n使用超模食物可能会导致游戏发生不可预料的错误\n本物品推荐价格为{0:f0}",
                        food.RealPrice),
                    LocalizeCore.Translate("超模食物/物品使用提醒"), this);
                if (!ok)
                    break;
                host.HostGameSave.HashCheckOff();
            }
            save.Money -= food.Price;
            host.FeedNoCharge(food);
        }
        RefreshMoney();
        RefreshShop();
        RefreshBag();
    }

    // ---- 背包 ----

    private void RefreshBag()
        => bagGrid.SetSource(host.HostItems.Items.Where(x => x.Visibility).ToList());

    private Control BuildItemCell(StoreItem item)
        => ShopCell.Build(
            $"{item.TranslateName} ×{item.Count}",
            null,
            null,
            item.Description,
            host.HostImages.Get(item.ImagePath),
            item.CanUse ? LocalizeCore.Translate("使用") : null,
            item.CanUse ? () => Use(item) : null);

    private void Use(StoreItem item)
    {
        if (!host.HostItems.Use(item))
            DialogService.Notice(
                LocalizeCore.Translate("物品 {0} 使用失败", item.TranslateName),
                LocalizeCore.Translate("该物品无法使用"), owner: this);
        RefreshBag();
        RefreshMoney();
    }

    // ---- 金钱与私房钱 ----

    private void RefreshMoney()
        => money.Text = LocalizeCore.Translate("金钱") + $": ${host.HostGameSave.GameSave.Money:f2}";

    /// <summary>
    /// 进店时看看要不要动私房钱
    /// </summary>
    /// 判定在共享后端里, 与 Windows 版是同一份
    private async void CheckLoan()
    {
        var save = host.HostGameSave.GameSave;
        const string selfKey = "self";
        var loaned = host.HostGameSave[(LinePutScript.gbol)selfKey];
        switch (PurchaseRules.CheckLoan(save.Money, loaned))
        {
            case PurchaseRules.LoanAction.RemindCredit:
                await DialogService.ShowAsync(LocalizeCore.Translate(
                    "更好买老顾客大优惠!桌宠的食物钱我来出!\n更好买提示您:$1000以下的食物/药品等随便赊账\n(不包括大于1000经验值的食物或礼品)"),
                    Title, this);
                break;
            case PurchaseRules.LoanAction.Give:
                await DialogService.ShowAsync(
                    LocalizeCore.Translate("看到您囊中羞涩,{0}拿出了1000块私房钱出来给你", save.Name), Title, this);
                host.HostGameSave[(LinePutScript.gbol)selfKey] = true;
                save.Money += PurchaseRules.LoanAmount;
                RefreshMoney();
                break;
            case PurchaseRules.LoanAction.TakeBack:
                save.Money -= PurchaseRules.LoanAmount;
                host.HostGameSave[(LinePutScript.gbol)selfKey] = false;
                RefreshMoney();
                await DialogService.ShowAsync(
                    LocalizeCore.Translate("{0}偷偷藏了1000块私房钱", save.Name), Title, this);
                break;
        }
    }
}
