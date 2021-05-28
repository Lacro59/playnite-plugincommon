using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace CommonPluginsShared
{
    public class CustomElement
    {
        public string ParentElementName { get; set; }
        public FrameworkElement Element { get; set; }
    }

    public abstract class PlayniteUiHelper
    {
        public static readonly ILogger logger = LogManager.GetLogger();
        public static IResourceProvider resources = new ResourceProvider();

        // TODO Used?
        /*
        public readonly IPlayniteAPI _PlayniteApi;
        public abstract string _PluginUserDataPath { get; set; }

        public IntegrationUI ui = new IntegrationUI();
        public readonly TaskHelper taskHelper = new TaskHelper();

        public abstract bool IsFirstLoad { get; set; }

        private StackPanel PART_ElemDescription = null;


        // BtActionBar
        public abstract string BtActionBarName { get; set; }
        public abstract FrameworkElement PART_BtActionBar { get; set; }

        // SpDescription
        public abstract string SpDescriptionName { get; set; }
        public abstract FrameworkElement PART_SpDescription { get; set; }

        // CustomElement
        public abstract List<CustomElement> ListCustomElements { get; set; }


        // SpInfoBarFS
        public abstract string SpInfoBarFSName { get; set; }
        public abstract FrameworkElement PART_SpInfoBarFS { get; set; }

        // BtActionBarFS
        public abstract string BtActionBarFSName { get; set; }
        public abstract FrameworkElement PART_BtActionBarFS { get; set; }



        public PlayniteUiHelper(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
        }


        public abstract void Initial();
        public abstract void RefreshElements(Game GameSelected, bool force = false);
        public void RemoveElements()
        {
            RemoveBtActionBar();
            RemoveSpDescription();
            RemoveCustomElements();

            RemoveSpInfoBarFS();
            RemoveBtActionBarFS();
        }


        #region DesktopMode
        public abstract DispatcherOperation AddElements();

        public abstract void InitialBtActionBar();
        public abstract void AddBtActionBar();
        public abstract void RefreshBtActionBar();
        public void RemoveBtActionBar()
        {
            if (!BtActionBarName.IsNullOrEmpty())
            {
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBarName);
                PART_BtActionBar = null;
            }
            else
            {
                logger.Warn($"RemoveBtActionBar() without BtActionBarName");
            }
        }


        public abstract void InitialSpDescription();
        public abstract void AddSpDescription();
        public abstract void RefreshSpDescription();
        public void RemoveSpDescription()
        {
            if (!SpDescriptionName.IsNullOrEmpty())
            {
                ui.RemoveElementInGameSelectedDescription(SpDescriptionName);
                PART_SpDescription = null;
                PART_ElemDescription = null;
            }
            else
            {
                logger.Warn($"RemoveSpDescription() without SpDescriptionrName");
            }
        }


        public abstract void InitialCustomElements();
        public abstract void AddCustomElements();
        public abstract void RefreshCustomElements();
        public void RemoveCustomElements()
        {
            foreach (CustomElement customElement in ListCustomElements)
            {
                ui.ClearElementInCustomTheme(customElement.ParentElementName);
            }
            ListCustomElements = new List<CustomElement>();
        }


        public void CheckTypeView()
        {
            if (PART_BtActionBar != null)
            {
                try
                {
                    FrameworkElement BtActionBarParent = (FrameworkElement)PART_BtActionBar.Parent;

                    if (BtActionBarParent != null)
                    {
                        if (!BtActionBarParent.IsVisible)
                        {
                            RemoveBtActionBar();
                        }
                    }
                    else
                    {
                        logger.Warn("BtActionBarParent is null");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on BtActionBar.CheckTypeView({PART_BtActionBar.Name})");
                }
            }

            if (PART_SpDescription != null)
            {
                try
                {
                    FrameworkElement SpDescriptionParent = (FrameworkElement)PART_SpDescription.Parent;

                    if (SpDescriptionParent != null)
                    {
                        if (!SpDescriptionParent.IsVisible)
                        {
                            RemoveSpDescription();
                        }
                    }
                    else
                    {
                        logger.Warn("SpDescriptionParent is null");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on SpDescription.CheckTypeView({PART_SpDescription.Name})");
                }
            }

            if (ListCustomElements.Count > 0)
            {
                bool isVisible = false;

                foreach (CustomElement customElement in ListCustomElements)
                {
                    try
                    {
                        FrameworkElement customElementParent = (FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)customElement.Element.Parent).Parent).Parent).Parent;

                        if (customElementParent != null)
                        {
                            // TODO Perfectible
                            if (!customElementParent.IsVisible)
                            {
                                //RemoveCustomElements();
                                //break;
                            }
                            else
                            {
                                isVisible = true;
                                break;
                            }
                        }
                        else
                        {
                            logger.Warn($"customElementParent is null for {customElement.Element.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error on customElement.CheckTypeView({customElement.Element.Name})");
                    }
                }

                if (!isVisible)
                {
                    RemoveCustomElements();
                }
            }
        }
        #endregion


        #region FullScreenMode
        public abstract DispatcherOperation AddElementsFS();

        public abstract void InitialSpInfoBarFS();
        public abstract void AddSpInfoBarFS();
        public abstract void RefreshSpInfoBarFS();
        public void RemoveSpInfoBarFS()
        {
            if (!SpInfoBarFSName.IsNullOrEmpty())
            {
                ui.RemoveStackPanelInGameSelectedInfoBarFS(SpInfoBarFSName);
                PART_SpInfoBarFS = null;
            }
            else
            {
                logger.Warn($"RemoveSpInfoBarFS() without SpInfoBarFSName");
            }
        }


        public abstract void InitialBtActionBarFS();
        public abstract void AddBtActionBarFS();
        public abstract void RefreshBtActionBarFS();
        public void RemoveBtActionBarFS()
        {
            if (!BtActionBarName.IsNullOrEmpty())
            {
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButtonFS(BtActionBarFSName);
                PART_BtActionBarFS = null;
            }
            else
            {
                logger.Warn($"RemoveBtActionBarFS() without BtActionBarFSName");
            }
        }
        #endregion


        public static void ResetToggle()
        {
            Common.LogDebug(true, "ResetToggle()");

            try
            {
                FrameworkElement PART_GaButton = IntegrationUI.SearchElementByName("PART_GaButton", true);
                if (PART_GaButton != null && PART_GaButton is ToggleButton && (bool)((ToggleButton)PART_GaButton).IsChecked)
                {
                    Common.LogDebug(true, "Reset PART_GaButton");

                    ((ToggleButton)PART_GaButton).IsChecked = false;
                    ((ToggleButton)PART_GaButton).RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                    return;
                }

                FrameworkElement PART_ScButton = IntegrationUI.SearchElementByName("PART_ScButton", true);
                if (PART_ScButton != null && PART_ScButton is ToggleButton && (bool)((ToggleButton)PART_ScButton).IsChecked)
                {
                    Common.LogDebug(true, "Reset PART_ScButton");

                    ((ToggleButton)PART_ScButton).IsChecked = false;
                    ((ToggleButton)PART_ScButton).RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                    return;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            Common.LogDebug(true, "No ResetToggle()");
        }


        public void OnBtActionBarToggleButtonClick(object sender, RoutedEventArgs e)
        {
            Common.LogDebug(true, $"OnBtActionBarToggleButtonClick()");

            try
            {
                if (PART_ElemDescription == null)
                {
                    PART_ElemDescription = (StackPanel)IntegrationUI.SearchElementByName("PART_ElemDescription", false, true);
                }

                if (PART_ElemDescription == null)
                {
                    logger.Warn("PART_ElemDescription not find on OnBtActionBarToggleButtonClick()");
                    return;
                }

                dynamic PART_ElemDescriptionParent = (FrameworkElement)PART_ElemDescription.Parent;

                //if (NotesVisibility == null)
                //{
                //    FrameworkElement PART_ElemNotes = IntegrationUI.SearchElementByName("PART_ElemNotes");
                //    if (PART_ElemNotes != null)
                //    {
                //        NotesVisibility = PART_ElemNotes.Visibility;
                //    }
                //}

                FrameworkElement PART_GaButton = IntegrationUI.SearchElementByName("PART_GaButton", true);
                FrameworkElement PART_ScButton = IntegrationUI.SearchElementByName("PART_ScButton", true);

                ToggleButton tgButton = sender as ToggleButton;

                if ((bool)(tgButton.IsChecked))
                {
                    for (int i = 0; i < PART_ElemDescriptionParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_GaDescriptionIntegration" && tgButton.Name == "PART_GaButton")
                        {
                            ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integration ToggleButton
                            if (PART_ScButton is ToggleButton)
                            {
                                ((ToggleButton)PART_ScButton).IsChecked = false;
                            }
                        }
                        else if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ScDescriptionIntegration" && tgButton.Name == "PART_ScButton")
                        {
                            ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integration ToggleButton
                            if (PART_GaButton is ToggleButton)
                            {
                                ((ToggleButton)PART_GaButton).IsChecked = false;
                            }
                        }
                        else
                        {
                            ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < PART_ElemDescriptionParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_GaDescriptionIntegration")
                        {
                            if (PART_GaButton is ToggleButton)
                            {
                                if ((bool)((ToggleButton)PART_GaButton).IsChecked)
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                                }
                            }
                            else
                            {
                                if (tgButton.Name == "PART_ScButton" && !(bool)tgButton.IsChecked)
                                {
                                    if ((string)((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Tag == "data")
                                    {
                                        ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                    }
                                }
                            }
                        }
                        else if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ScDescriptionIntegration")
                        {
                            if (PART_ScButton is ToggleButton)
                            {
                                if ((bool)((ToggleButton)PART_ScButton).IsChecked)
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                                }
                            }
                            else
                            {
                                if (tgButton.Name == "PART_GaButton" && !(bool)tgButton.IsChecked)
                                {
                                    if ((string)((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Tag == "data")
                                    {
                                        ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ElemNotes")
                            {
                                FrameworkElement PART_TextNotes = (FrameworkElement)((FrameworkElement)PART_ElemDescriptionParent.Children[i]).FindName("PART_TextNotes");

                                ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Collapsed;
                                if (PART_TextNotes != null)
                                {
                                    if (PART_TextNotes is TextBox && ((TextBox)PART_TextNotes).Text != string.Empty)
                                    {
                                        ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                                    }
                                }
                            }
                            else if (((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Name == "PART_ElemDescription")
                            {
                                ((FrameworkElement)PART_ElemDescriptionParent.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
        */

        public static void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (sender is Window)
                {
                    e.Handled = true;
                    ((Window)sender).Close();
                }
            }
            else
            {
            }
        }

        public static Window CreateExtensionWindow(IPlayniteAPI PlayniteApi, string Title, UserControl ViewExtension,
            WindowCreationOptions windowCreationOptions = null)
        {
            if (windowCreationOptions == null)
            {
                windowCreationOptions = new WindowCreationOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = true
                };
            }

            Window windowExtension = PlayniteApi.Dialogs.CreateWindow(windowCreationOptions);

            windowExtension.Title = Title;
            windowExtension.ShowInTaskbar = false;
            windowExtension.ResizeMode = ResizeMode.NoResize;
            windowExtension.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            windowExtension.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            windowExtension.Content = ViewExtension;

            if (!double.IsNaN(ViewExtension.Height) && !double.IsNaN(ViewExtension.Width))
            {
                windowExtension.Height = ViewExtension.Height + 25;
                windowExtension.Width = ViewExtension.Width;
            }
            else if (!double.IsNaN(ViewExtension.MinHeight) && !double.IsNaN(ViewExtension.MinWidth) && ViewExtension.MinHeight > 0 && ViewExtension.MinWidth > 0)
            {
                windowExtension.Height = ViewExtension.MinHeight + 25;
                windowExtension.Width = ViewExtension.MinWidth;
            }
            else
            {
                windowExtension.SizeToContent = SizeToContent.WidthAndHeight;
            }

            windowExtension.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            return windowExtension;
        }
    }


    public class IntegrationUI
    {
        private static ILogger logger = LogManager.GetLogger();


        public bool AddResources(List<ResourcesList> ResourcesList)
        {
            Common.LogDebug(true, $"AddResources() - {JsonConvert.SerializeObject(ResourcesList)}");

            string ItemKey = string.Empty;

            foreach (ResourcesList item in ResourcesList)
            {
                try
                {
                    ItemKey = item.Key;
                    if (Application.Current.Resources[ItemKey] != null)
                    {
                        Type TypeActual = Application.Current.Resources[ItemKey].GetType();
                        Type TypeNew = item.Value.GetType();

                        if (TypeActual != TypeNew)
                        {
                            if ((TypeActual.Name == "SolidColorBrush" || TypeActual.Name == "LinearGradientBrush")
                                && (TypeNew.Name == "SolidColorBrush" || TypeNew.Name == "LinearGradientBrush"))
                            {
                            }
                            else
                            {
                                logger.Warn($"Different type for {ItemKey}");
                                continue;
                            }
                        }

                        Application.Current.Resources[ItemKey] = item.Value;
                    }
                    else
                    {
                        Application.Current.Resources.Add(item.Key, item.Value);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Error in AddResources({ItemKey})");
                    Common.LogError(ex, true, $"Error in AddResources({ItemKey})");
                }
            }
            return true;
        }


        private static FrameworkElement SearchElementByNameInExtander(object control, string ElementName)
        {
            if (control is FrameworkElement)
            {
                if (((FrameworkElement)control).Name == ElementName)
                {
                    return (FrameworkElement)control;
                }


                var children = LogicalTreeHelper.GetChildren((FrameworkElement)control);
                foreach (object child in children)
                {
                    if (child is FrameworkElement)
                    {
                        if (((FrameworkElement)child).Name == ElementName)
                        {
                            return (FrameworkElement)child;
                        }

                        var subItems = LogicalTreeHelper.GetChildren((FrameworkElement)child);
                        foreach (object subItem in subItems)
                        {
                            if (subItem.ToString().ToLower().Contains("expander") || subItem.ToString().ToLower().Contains("tabitem"))
                            {
                                FrameworkElement tmp = null;

                                if (subItem.ToString().ToLower().Contains("expander"))
                                {
                                    tmp = SearchElementByNameInExtander(((Expander)subItem).Content, ElementName);
                                }

                                if (subItem.ToString().ToLower().Contains("tabitem"))
                                {
                                    tmp = SearchElementByNameInExtander(((TabItem)subItem).Content, ElementName);
                                }

                                if (tmp != null)
                                {
                                    return tmp;
                                }
                            }
                            else
                            {
                                var tmp = SearchElementByNameInExtander(child, ElementName);
                                if (tmp != null)
                                {
                                    return tmp;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static FrameworkElement SearchElementByName(string ElementName, bool MustVisible = false, bool ParentMustVisible = false, int counter = 1)
        {
            return SearchElementByName(ElementName, Application.Current.MainWindow, MustVisible, ParentMustVisible, counter);
        }

        public static FrameworkElement SearchElementByName(string ElementName, DependencyObject dpObj, bool MustVisible = false, bool ParentMustVisible = false, int counter = 1)
        {
            FrameworkElement ElementFind = null;

            int count = 0;

            if (ElementFind == null)
            {
                foreach (FrameworkElement el in Tools.FindVisualChildren<FrameworkElement>(dpObj))
                {
                    if (el.ToString().ToLower().Contains("expander") || el.ToString().ToLower().Contains("tabitem"))
                    {
                        FrameworkElement tmpEl = null;

                        if (el.ToString().ToLower().Contains("expander"))
                        {
                            tmpEl = SearchElementByNameInExtander(((Expander)el).Content, ElementName);
                        }

                        if (el.ToString().ToLower().Contains("tabitem"))
                        {
                            tmpEl = SearchElementByNameInExtander(((TabItem)el).Content, ElementName);
                        }

                        if (tmpEl != null)
                        {
                            if (tmpEl.Name == ElementName)
                            {
                                if (!MustVisible)
                                {
                                    if (!ParentMustVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                    else if (((FrameworkElement)el.Parent).IsVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                }
                                else if (tmpEl.IsVisible)
                                {
                                    if (!ParentMustVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                    else if (((FrameworkElement)el.Parent).IsVisible)
                                    {
                                        ElementFind = tmpEl;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (el.Name == ElementName)
                    {
                        count++;

                        if (!MustVisible)
                        {
                            if (!ParentMustVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                            else if (((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)el.Parent).Parent).Parent).Parent).Parent).IsVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                        }
                        else if (el.IsVisible)
                        {
                            if (!ParentMustVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                            else if (((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)el.Parent).Parent).Parent).Parent).Parent).IsVisible)
                            {
                                if (count == counter)
                                {
                                    ElementFind = el;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return ElementFind;
        }


        private static bool SearchElementInsert(List<FrameworkElement> SearchList, string ElSearchName)
        {
            foreach (FrameworkElement el in SearchList)
            {
                if (ElSearchName == el.Name)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SearchElementInsert(List<FrameworkElement> SearchList, FrameworkElement ElSearch)
        {
            foreach (FrameworkElement el in SearchList)
            {
                if (ElSearch.Name == el.Name)
                {
                    return true;
                }
            }
            return false;
        }


        public static T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent != null && !(parent is T))
            {
                return (T)GetAncestorOfType<T>((FrameworkElement)parent);
            }
            return (T)parent;
        }


        public static void SetControlSize(FrameworkElement ControlElement)
        {
            SetControlSize(ControlElement, 0, 0);
        }

        public static void SetControlSize(FrameworkElement ControlElement, double DefaultHeight, double DefaultWidth)
        {
            try
            {
                UserControl ControlParent = IntegrationUI.GetAncestorOfType<UserControl>(ControlElement);
                FrameworkElement ControlContener = (FrameworkElement)ControlParent.Parent;

                Common.LogDebug(true, $"SetControlSize({ControlElement.Name}) - parent.name: {ControlContener.Name} - parent.Height: {ControlContener.Height} - parent.Width: {ControlContener.Width} - parent.MaxHeight: {ControlContener.MaxHeight} - parent.MaxWidth: {ControlContener.MaxWidth}");

                // Set Height
                if (!double.IsNaN(ControlContener.Height))
                {
                    ControlElement.Height = ControlContener.Height;
                }
                else if (DefaultHeight != 0)
                {
                    ControlElement.Height = DefaultHeight;
                }
                // Control with MaxHeight
                if (!double.IsNaN(ControlContener.MaxHeight))
                {
                    if (ControlElement.Height > ControlContener.MaxHeight)
                    {
                        ControlElement.Height = ControlContener.MaxHeight;
                    }
                }


                // Set Width
                if (!double.IsNaN(ControlContener.Width))
                {
                    ControlElement.Width = ControlContener.Width;
                }
                else if (DefaultWidth != 0)
                {
                    ControlElement.Width = DefaultWidth;
                }
                // Control with MaxWidth
                if (!double.IsNaN(ControlContener.MaxWidth))
                {
                    if (ControlElement.Width > ControlContener.MaxWidth)
                    {
                        ControlElement.Width = ControlContener.MaxWidth;
                    }
                }
            }
            catch
            {

            }
        }
    }


    public class ResourcesList
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }
}
