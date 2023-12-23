using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HKW.WPF.Extensions;

public static partial class WPFExtensions
{
    /// <summary>
    /// 清除元素焦点
    /// </summary>
    /// <param name="obj">元素</param>
    public static void ClearFocus(this DependencyObject obj)
    {
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(obj), null);
    }
}
