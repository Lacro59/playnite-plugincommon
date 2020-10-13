using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace PluginCommon
{
    public enum ParentTypeView : int
    {
        Details = 0,
        Grid = 1,
        List = 2,
        Unknown = 9
    }

    public class CustomElement
    {
        public string ParentElementName { get; set; }
        public FrameworkElement Element { get; set; }
    }

    public abstract class PlayniteUiHelper
    {
        public static readonly ILogger logger = LogManager.GetLogger();
        public static IResourceProvider resources = new ResourceProvider();

        public readonly IPlayniteAPI _PlayniteApi;
        public readonly string _PluginUserDataPath;

        public IntegrationUI ui = new IntegrationUI();
        public readonly TaskHelper taskHelper = new TaskHelper();

        public bool IsFirstLoad { get; set; } = true;

        // BtActionBar
        public static string BtActionBarName = string.Empty;
        public FrameworkElement PART_BtActionBar;
        public ParentTypeView BtActionBarParentType = ParentTypeView.Unknown;

        // CustomElement
        public List<CustomElement> ListCustomElements = new List<CustomElement>();


        public PlayniteUiHelper(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;
        }


        public abstract void Initial();
        public abstract void AddElements();
        public abstract void RefreshElements(Game GameSelected);
        public void RemoveElements()
        {
            RemoveBtActionBar();
            RemoveCustomElements();
        }


        public abstract void InitialBtActionBar();
        public abstract void AddBtActionBar();
        public abstract void RefreshBtActionBar();
        public void RemoveBtActionBar()
        {
            if (!BtActionBarName.IsNullOrEmpty())
            {
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBarName);
                PART_BtActionBar = null;
                BtActionBarParentType = ParentTypeView.Unknown;
            }
            else
            {
                logger.Warn($"PluginCommon - RemoveBtActionBar() without BtActionBarName");
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
            // TODO Don't work when you change view
            if (PART_BtActionBar != null)
            {
                FrameworkElement BtActionBarParent = (FrameworkElement)PART_BtActionBar.Parent;
                if (BtActionBarParent is StackPanel && BtActionBarParentType != ParentTypeView.Details)
                {
#if DEBUG
                    logger.Debug($"PluginCommon - PART_BtActionBar removed from DetailsView");
#endif
                    RemoveBtActionBar();
                    BtActionBarParentType = ParentTypeView.Details;
                }
                if (BtActionBarParent is Grid && BtActionBarParentType != ParentTypeView.Grid)
                {
#if DEBUG
                    logger.Debug($"PluginCommon - PART_BtActionBar removed from GridView");
#endif
                    RemoveBtActionBar();
                    BtActionBarParentType = ParentTypeView.Grid;
                }
            }
        }


        public static void HandleEsc(object sender, KeyEventArgs e)
        {
#if DEBUG
            logger.Debug($"PluginCommon - {sender.ToString()}");
#endif

            if (e.Key == Key.Escape)
            {
                if (sender is Window)
                {
                    ((Window)sender).Close();
                }
            }
            else
            {
#if DEBUG
                logger.Debug("PluginCommon - sender is not a Window");
#endif
            }
        }

        public static Window CreateExtensionWindow(IPlayniteAPI PlayniteApi, string Title, UserControl ViewExtension)
        {
            Window windowExtension = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true
            });
            windowExtension.Title = Title;
            windowExtension.ShowInTaskbar = false;
            windowExtension.ResizeMode = ResizeMode.NoResize;
            windowExtension.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            windowExtension.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            windowExtension.Content = ViewExtension;
            windowExtension.Width = ViewExtension.Width;
            windowExtension.Height = ViewExtension.Height + 25;
            windowExtension.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            return windowExtension;
        }
    }


    public class IntegrationUI
    {
        private static ILogger logger = LogManager.GetLogger();


        public bool AddResources(List<ResourcesList> ResourcesList)
        {
            string ItemKey = string.Empty;
            try
            {
                foreach (ResourcesList item in ResourcesList)
                {
                    ItemKey = item.Key;
                    Application.Current.Resources.Remove(item.Key);
                    Application.Current.Resources.Add(item.Key, item.Value);
                }
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddResources({ItemKey})");
                return false;
            }
        }


        #region Header button
        private FrameworkElement btHeaderChild = null;
        private List<FrameworkElement> btHeaderList = new List<FrameworkElement>();

        public bool AddButtonInWindowsHeader(Button btHeader)
        {
            try
            {
                // Add only if not exist
                if (!SearchElementInsert(btHeaderList, btHeader))
                {
                    // Add search element if not allready find
                    if (btHeaderChild == null)
                    {
                        btHeaderChild = SearchElementByName("PART_ButtonSteamFriends");
                    }

                    // Not find element
                    if (btHeaderChild == null)
                    {
                        logger.Error("PluginCommon - btHeaderChild [PART_ButtonSteamFriends] not find");
                        return false;
                    }

                    // Add in parent if good type
                    if (btHeaderChild.Parent is DockPanel)
                    {
                        btHeader.Width = btHeaderChild.ActualWidth;
                        btHeader.Height = btHeaderChild.ActualHeight;
                        DockPanel.SetDock(btHeader, Dock.Right);

                        // Add button 
                        DockPanel btHeaderParent = (DockPanel)btHeaderChild.Parent;
                        for (int i = 0; i < btHeaderParent.Children.Count; i++)
                        {
                            if (((FrameworkElement)btHeaderParent.Children[i]).Name == "PART_ButtonSteamFriends")
                            {
                                btHeaderParent.Children.Insert((i - 1), btHeader);
                                btHeaderParent.UpdateLayout();
                                i = btHeaderParent.Children.Count;

#if DEBUG
                                logger.Debug($"PluginCommon - btHeader [{btHeader.Name}] insert");
#endif
                            }
                        }
                    }
                    else
                    {
                        logger.Error("PluginCommon - btHeaderChild.Parent is not a DockPanel element");
                        return false;
                    }
                }
                else
                {
#if DEBUG
                    logger.Debug($"PluginCommon - btHeader [{btHeader.Name}] allready insert");
#endif
                }

                btHeaderList.Add(btHeader);
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddButtonInWindowsHeader({btHeader.Name})");
                return false;
            }
        }
        #endregion


        #region GameSelectedActionBar button
        private FrameworkElement btGameSelectedActionBarChild = null;
        private List<FrameworkElement> btGameSelectedActionBarList = new List<FrameworkElement>();

        public bool AddButtonInGameSelectedActionBarButtonOrToggleButton(FrameworkElement btGameSelectedActionBar)
        {
            try
            {
                // Add only if not exist
                if (!SearchElementInsert(btGameSelectedActionBarList, btGameSelectedActionBar))
                {
                    btGameSelectedActionBarChild = SearchElementByName("PART_ButtonEditGame");

                    // Not find element
                    if (btGameSelectedActionBarChild == null)
                    {
                        logger.Error("PluginCommon - btGameSelectedActionBarChild [PART_ButtonEditGame] not find");
                        return false;
                    }

                    btGameSelectedActionBar.Height = btGameSelectedActionBarChild.ActualHeight;
                    if (btGameSelectedActionBar.Name == "PART_HltbButton")
                    {
                        btGameSelectedActionBar.Width = btGameSelectedActionBarChild.ActualWidth;
                    }

                    // Not find element
                    if (btGameSelectedActionBarChild == null)
                    {
                        logger.Error("PluginCommon - btGameSelectedActionBarChild [PART_ButtonEditGame] not find");
                        return false;
                    }

                    // Add in parent if good type
                    if (btGameSelectedActionBarChild.Parent is StackPanel)
                    {
                        // Add button 
                        ((StackPanel)(btGameSelectedActionBarChild.Parent)).Children.Add(btGameSelectedActionBar);
                        ((StackPanel)(btGameSelectedActionBarChild.Parent)).UpdateLayout();

#if DEBUG
                        logger.Debug($"PluginCommon - (StackPanel)btGameSelectedActionBar [{btGameSelectedActionBar.Name}] insert");
#endif
                    }

                    if (btGameSelectedActionBarChild.Parent is Grid)
                    {
                        StackPanel spContener = (StackPanel)SearchElementByName("PART_spContener");

                        // Add StackPanel contener
                        if (((Grid)btGameSelectedActionBarChild.Parent).ColumnDefinitions.Count == 3)
                        {
                            var columnDefinitions = new ColumnDefinition();
                            columnDefinitions.Width = GridLength.Auto;
                            ((Grid)(btGameSelectedActionBarChild.Parent)).ColumnDefinitions.Add(columnDefinitions);

                            spContener = new StackPanel();
                            spContener.Name = "PART_spContener";
                            spContener.Orientation = Orientation.Horizontal;
                            spContener.SetValue(Grid.ColumnProperty, 3);

                            btGameSelectedActionBarChild.Margin = new Thickness(10, 0, 0, 0);

                            ((Grid)(btGameSelectedActionBarChild.Parent)).Children.Add(spContener);
                            ((Grid)(btGameSelectedActionBarChild.Parent)).UpdateLayout();
                        }

                        // Add button 
                        spContener.Children.Add(btGameSelectedActionBar);
                        spContener.UpdateLayout();

#if DEBUG
                        logger.Debug($"PluginCommon - (Grid)btGameSelectedActionBar [{btGameSelectedActionBar.Name}] insert");
#endif

                    }
                }
                else
                {
#if DEBUG
                    logger.Debug($"PluginCommon - btGameSelectedActionBar [{btGameSelectedActionBar.Name}] allready insert");
#endif
                }

                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBar.Name})");
                return false;
            }
        }

        public bool RemoveButtonInGameSelectedActionBarButtonOrToggleButton(string btGameSelectedActionBarName)
        {
            try
            {
                btGameSelectedActionBarChild = SearchElementByName("PART_ButtonEditGame");
                FrameworkElement spContener = SearchElementByName("PART_spContener");

                // Not find element
                if (btGameSelectedActionBarChild == null)
                {
                    logger.Error("PluginCommon - btGameSelectedActionBarChild [PART_ButtonEditGame] not find");
                    return false;
                }

                // Remove in parent if good type
                if (btGameSelectedActionBarChild.Parent is StackPanel && btGameSelectedActionBarChild != null)
                {
                    StackPanel btGameSelectedParent = ((StackPanel)(btGameSelectedActionBarChild.Parent));
                    for (int i = 0; i < btGameSelectedParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)btGameSelectedParent.Children[i]).Name == btGameSelectedActionBarName)
                        {
                            btGameSelectedParent.Children.Remove(btGameSelectedParent.Children[i]);
                            btGameSelectedParent.UpdateLayout();

#if DEBUG
                            logger.Debug($"PluginCommon - (StackPanel)btGameSelectedActionBar [{btGameSelectedActionBarName}] remove");
#endif
                        }
                    }
                }

                if (btGameSelectedActionBarChild.Parent is Grid && spContener != null)
                {
                    for (int i = 0; i < ((StackPanel)spContener).Children.Count; i++)
                    {
                        if (((FrameworkElement)((StackPanel)spContener).Children[i]).Name == btGameSelectedActionBarName)
                        {
                            ((StackPanel)spContener).Children.Remove(((StackPanel)spContener).Children[i]);
                            spContener.UpdateLayout();

#if DEBUG
                            logger.Debug($"PluginCommon - (Grid)btGameSelectedActionBar [{btGameSelectedActionBarName}] remove");
#endif
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in RemoveButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBarName})");
                return false;
            }
        }
        #endregion


        #region GameSelectedDescription
        private FrameworkElement elGameSelectedDescriptionContener = null;
        private List<FrameworkElement> elGameSelectedDescriptionList = new List<FrameworkElement>();

        public bool AddElementInGameSelectedDescription(FrameworkElement elGameSelectedDescription, bool isTop = false)
        {
            try
            {
                // Add only if not exist
                if (!SearchElementInsert(elGameSelectedDescriptionList, elGameSelectedDescription))
                {
                    elGameSelectedDescriptionContener = SearchElementByName("PART_HtmlDescription");
                    elGameSelectedDescriptionContener = (FrameworkElement)elGameSelectedDescriptionContener.Parent;

                    // Not find element
                    if (elGameSelectedDescriptionContener == null)
                    {
                        logger.Error("PluginCommon - elGameSelectedDescriptionContener [PART_ElemDescription] not find");
                        return false;
                    }

                    // Add in parent if good type
                    if (elGameSelectedDescriptionContener is StackPanel)
                    {
                        // Add FrameworkElement 
                        if (isTop)
                        {
                            ((StackPanel)elGameSelectedDescriptionContener).Children.Insert(0, elGameSelectedDescription);
                        }
                        else
                        {
                            ((StackPanel)elGameSelectedDescriptionContener).Children.Add(elGameSelectedDescription);
                        }
                        ((StackPanel)elGameSelectedDescriptionContener).UpdateLayout();

#if DEBUG
                        logger.Debug($"PluginCommon - elGameSelectedDescriptionContener [{elGameSelectedDescription.Name}] insert");
#endif
                    }

                    if (elGameSelectedDescriptionContener is DockPanel)
                    {
                        elGameSelectedDescription.SetValue(DockPanel.DockProperty, Dock.Top);

                        // Add FrameworkElement 
                        if (isTop)
                        {
                            ((DockPanel)elGameSelectedDescriptionContener).Children.Insert(1, elGameSelectedDescription);
                        }
                        else
                        {
                            ((DockPanel)elGameSelectedDescriptionContener).Children.Add(elGameSelectedDescription);
                        }
                        ((DockPanel)elGameSelectedDescriptionContener).UpdateLayout();

#if DEBUG
                        logger.Debug($"PluginCommon - elGameSelectedDescriptionContener [{elGameSelectedDescription.Name}] insert");
#endif
                    }
                }
                else
                {
#if DEBUG
                    logger.Debug($"PluginCommon - elGameSelectedDescription [{elGameSelectedDescription.Name}] allready insert");
#endif
                }

                elGameSelectedDescriptionList.Add(elGameSelectedDescription);
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddElementInGameSelectedDescription({elGameSelectedDescription.Name})");
                return false;
            }
        }

        public bool RemoveElementInGameSelectedDescription(string elGameSelectedDescriptionName)
        {
            try
            {
                // Remove only if not exist
                if (SearchElementInsert(elGameSelectedDescriptionList, elGameSelectedDescriptionName))
                {
                    elGameSelectedDescriptionContener = SearchElementByName("PART_HtmlDescription");
                    elGameSelectedDescriptionContener = (FrameworkElement)elGameSelectedDescriptionContener.Parent;

                    // Not find element
                    if (elGameSelectedDescriptionContener == null)
                    {
                        logger.Error("PluginCommon - elGameSelectedDescriptionContener [PART_ElemDescription] not find");
                        return false;
                    }

                    // Remove in parent if good type
                    if (elGameSelectedDescriptionContener is StackPanel)
                    {
                        StackPanel elGameSelectedParent = ((StackPanel)(elGameSelectedDescriptionContener));
                        for (int i = 0; i < elGameSelectedParent.Children.Count; i++)
                        {
                            if (((FrameworkElement)elGameSelectedParent.Children[i]).Name == elGameSelectedDescriptionName)
                            {
                                elGameSelectedParent.Children.Remove(elGameSelectedParent.Children[i]);
                                elGameSelectedParent.UpdateLayout();

#if DEBUG
                                logger.Debug($"PluginCommon - elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] remove");
#endif

                                foreach (FrameworkElement el in elGameSelectedDescriptionList)
                                {
                                    if (elGameSelectedDescriptionName == el.Name)
                                    {
                                        elGameSelectedDescriptionList.Remove(el);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (elGameSelectedDescriptionContener is DockPanel)
                    {
                        DockPanel elGameSelectedParent = ((DockPanel)(elGameSelectedDescriptionContener));
                        for (int i = 0; i < elGameSelectedParent.Children.Count; i++)
                        {
                            if (((FrameworkElement)elGameSelectedParent.Children[i]).Name == elGameSelectedDescriptionName)
                            {
                                elGameSelectedParent.Children.Remove(elGameSelectedParent.Children[i]);
                                elGameSelectedParent.UpdateLayout();

#if DEBUG
                                logger.Debug($"PluginCommon - elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] remove");
#endif

                                foreach (FrameworkElement el in elGameSelectedDescriptionList)
                                {
                                    if (elGameSelectedDescriptionName == el.Name)
                                    {
                                        elGameSelectedDescriptionList.Remove(el);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
#if DEBUG
                    logger.Debug($"PluginCommon - elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] allready remove");
#endif
                }

                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in RemoveElementInGameSelectedDescription({elGameSelectedDescriptionName})");
                return false;
            }
        }
        #endregion


        #region Custom theme
        public bool AddElementInCustomTheme(FrameworkElement ElementInCustomTheme, string ElementParentInCustomThemeName)
        {
            try
            {
                FrameworkElement ElementCustomTheme = SearchElementByName(ElementParentInCustomThemeName);

                // Not find element
                if (ElementCustomTheme == null)
                {
                    logger.Error($"PluginCommon - ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                    return false;
                }

                // Add in parent if good type
                if (ElementCustomTheme is StackPanel)
                {
                    if (!double.IsNaN(ElementCustomTheme.Height))
                    {
                        ElementInCustomTheme.Height = ElementCustomTheme.Height;
                    }

                    if (!double.IsNaN(ElementCustomTheme.Width))
                    {
                        ElementInCustomTheme.Width = ElementCustomTheme.Width;
                    }

                    // Add FrameworkElement 
                    ((StackPanel)ElementCustomTheme).Children.Add(ElementInCustomTheme);
                    ((StackPanel)ElementCustomTheme).Visibility = Visibility.Visible;
                    ((StackPanel)ElementCustomTheme).UpdateLayout();

#if DEBUG
                    logger.Debug($"PluginCommon - ElementCustomTheme [{ElementCustomTheme.Name}] insert");
#endif
                }
                else
                {
                    logger.Error($"PluginCommon - ElementCustomTheme is not a StackPanel element");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddElementInCustomTheme({ElementParentInCustomThemeName})");
                return false;
            }
        }

        public bool ClearElementInCustomTheme(string ElementParentInCustomThemeName)
        {
            try
            {
                FrameworkElement ElementCustomTheme = SearchElementByName(ElementParentInCustomThemeName);

                // Not find element
                if (ElementCustomTheme == null)
                {
                    logger.Error($"PluginCommon - ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                    return false;
                }

                // Add in parent if good type
                if (ElementCustomTheme is StackPanel)
                {
                    // Clear FrameworkElement 
                    ((StackPanel)ElementCustomTheme).Children.Clear();
                    ((StackPanel)ElementCustomTheme).Visibility = Visibility.Collapsed;
                    ((StackPanel)ElementCustomTheme).UpdateLayout();

#if DEBUG
                    logger.Debug($"PluginCommon - ElementCustomTheme [{ElementCustomTheme.Name}] clear");
#endif
                }
                else
                {
                    logger.Error($"PluginCommon - ElementCustomTheme is not a StackPanel element");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in ClearElementInCustomTheme({ElementParentInCustomThemeName})");
                return false;
            }
        }
        #endregion


        public static FrameworkElement SearchElementByName(string ElementName)
        {
            FrameworkElement ElementFind = null;

            ElementFind = (FrameworkElement)LogicalTreeHelper.FindLogicalNode(Application.Current.MainWindow, ElementName);

            if (ElementFind == null)
            {
                foreach (FrameworkElement el in Tools.FindVisualChildren<FrameworkElement>(Application.Current.MainWindow))
                {
                    if (el.Name == ElementName)
                    {
                        ElementFind = el;
                        break;
                    }
                }
            }

            return ElementFind;
        }

        public static FrameworkElement SearchElementByName(string ElementName, DependencyObject dpObj)
        {
            FrameworkElement ElementFind = null;

            ElementFind = (FrameworkElement)LogicalTreeHelper.FindLogicalNode(dpObj, ElementName);

            if (ElementFind == null)
            {
                foreach (FrameworkElement el in Tools.FindVisualChildren<FrameworkElement>(dpObj))
                {
                    if (el.Name == ElementName)
                    {
                        ElementFind = el;
                        break;
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
    }


    public class ResourcesList
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }
}
