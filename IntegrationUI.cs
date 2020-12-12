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

namespace PluginCommon
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
                logger.Warn($"PluginCommon - RemoveBtActionBar() without BtActionBarName");
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
            }
            else
            {
                logger.Warn($"PluginCommon - RemoveSpDescription() without SpDescriptionrName");
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
#if DEBUG
                        logger.Debug($"PluginCommon - BtActionBarParent: {BtActionBarParent.ToString()} - {BtActionBarParent.IsVisible}");
#endif
                        if (!BtActionBarParent.IsVisible)
                        {
                            RemoveBtActionBar();
                        }
                    }
                    else
                    {
                        logger.Warn("PluginCommon - BtActionBarParent is null");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "PluginCommon", $"Error in BtActionBar.CheckTypeView({PART_BtActionBar.Name})");
                }
            }

            if (PART_SpDescription != null)
            {
                try
                {
                    FrameworkElement SpDescriptionParent = (FrameworkElement)PART_SpDescription.Parent;

                    if (SpDescriptionParent != null)
                    {
#if DEBUG
                        logger.Debug($"PluginCommon - SpDescriptionParent: {SpDescriptionParent.ToString()} - {SpDescriptionParent.IsVisible}");
#endif
                        if (!SpDescriptionParent.IsVisible)
                        {
                            RemoveSpDescription();
                        }
                    }
                    else
                    {
                        logger.Warn("PluginCommon - SpDescriptionParent is null");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "PluginCommon", $"Error in SpDescription.CheckTypeView({PART_SpDescription.Name})");
                }
            }

            foreach (CustomElement customElement in ListCustomElements)
            {
                try
                {
                    FrameworkElement customElementParent = (FrameworkElement)customElement.Element.Parent;

                    if (customElementParent != null)
                    {
#if DEBUG
                        logger.Debug($"PluginCommon - SpDescriptionParent: {customElementParent.ToString()} - {customElementParent.IsVisible}");
#endif
                        if (!customElementParent.IsVisible)
                        {
                            RemoveCustomElements();
                            break;
                        }
                    }
                    else
                    {
                        logger.Warn($"PluginCommon - customElementParent is null for {customElement.Element.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "PluginCommon", $"Error in customElement.CheckTypeView({customElement.Element.Name})");
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
                logger.Warn($"PluginCommon - RemoveSpInfoBarFS() without SpInfoBarFSName");
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
                logger.Warn($"PluginCommon - RemoveBtActionBarFS() without BtActionBarFSName");
            }
        }
        #endregion


        public static void HandleEsc(object sender, KeyEventArgs e)
        {
#if DEBUG
            logger.Debug($"PluginCommon - {sender.ToString()}");
#endif
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

            if (!double.IsNaN(ViewExtension.Height) && !double.IsNaN(ViewExtension.Width))
            {
                windowExtension.Height = ViewExtension.Height + 25;
                windowExtension.Width = ViewExtension.Width;
            }
            else
            {
                windowExtension.SizeToContent = SizeToContent.WidthAndHeight;
            }

            windowExtension.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            return windowExtension;
        }


        public static void ResetToggle()
        {
#if DEBUG
            logger.Debug("PluginCommon - ResetToggle()");
#endif
            try
            {
                FrameworkElement PART_GaButton = IntegrationUI.SearchElementByName("PART_GaButton", true);
                if (PART_GaButton != null)
                {
#if DEBUG
                    logger.Debug("PluginCommon - Reset PART_GaButton");
#endif
                    ((ToggleButton)PART_GaButton).IsChecked = false;
                    ((ToggleButton)PART_GaButton).RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                    return;
                }

                FrameworkElement PART_ScButton = IntegrationUI.SearchElementByName("PART_ScButton", true);
                if (PART_ScButton != null && PART_ScButton is ToggleButton && (bool)((ToggleButton)PART_ScButton).IsChecked)
                {
#if DEBUG
                    logger.Debug("PluginCommon - Reset PART_ScButton");
#endif
                    ((ToggleButton)PART_ScButton).IsChecked = false;
                    ((ToggleButton)PART_ScButton).RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
                    return;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on ResetToggle()");
            }
#if DEBUG
            logger.Debug("PluginCommon - No ResetToggle()");
#endif
        }


        public void OnBtActionBarToggleButtonClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            logger.Debug($"PluginCommon - OnBtActionBarToggleButtonClick()");
#endif
            if (PART_ElemDescription == null)
            {
                foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                {
                    if (sp.Name == "PART_ElemDescription")
                    {
                        PART_ElemDescription = sp;
                        break;
                    }
                }
            }

            if (PART_ElemDescription != null)
            {
                FrameworkElement PART_GaButton = IntegrationUI.SearchElementByName("PART_GaButton", true);
                FrameworkElement PART_ScButton = IntegrationUI.SearchElementByName("PART_ScButton", true);

                ToggleButton tgButton = sender as ToggleButton;

                if ((bool)(tgButton.IsChecked))
                {
                    for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_GaDescriptionIntegration" && tgButton.Name == "PART_GaButton")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integration ToggleButton
                            if (PART_ScButton is ToggleButton)
                            {
                                ((ToggleButton)PART_ScButton).IsChecked = false;
                            }
                        }
                        else if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_ScDescriptionIntegration" && tgButton.Name == "PART_ScButton")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integration ToggleButton
                            if (PART_GaButton is ToggleButton)
                            {
                                ((ToggleButton)PART_GaButton).IsChecked = false;
                            }
                        }
                        else
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_GaDescriptionIntegration")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Collapsed;
                            if (tgButton.Name == "PART_GaButton" && (bool)tgButton.IsChecked)
                            {
                                ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                        else if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_ScDescriptionIntegration")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Collapsed;
                            if (tgButton.Name == "PART_ScButton" && (bool)tgButton.IsChecked)
                            {
                                ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            else
            {
                logger.Error("PluginCommon - PART_ElemDescription not found");
            }
        }
    }


    public class IntegrationUI
    {
        private static ILogger logger = LogManager.GetLogger();


        public bool AddResources(List<ResourcesList> ResourcesList)
        {
#if DEBUG
            logger.Debug($"PluginCommon - AddResources() - {JsonConvert.SerializeObject(ResourcesList)}");
#endif
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

        public void AddButtonInWindowsHeader(Button btHeader)
        {
            try
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
                    return;
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
                    return;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddButtonInWindowsHeader({btHeader.Name})");
                return;
            }
        }
        #endregion


        #region GameSelectedActionBar button
        private FrameworkElement btGameSelectedActionBarChild = null;

        public void AddButtonInGameSelectedActionBarButtonOrToggleButton(FrameworkElement btGameSelectedActionBar)
        {
            try
            {

                btGameSelectedActionBarChild = SearchElementByName("PART_ButtonMoreActions", true);

                // Not find element
                if (btGameSelectedActionBarChild == null)
                {
                    logger.Error("PluginCommon - btGameSelectedActionBarChild [PART_ButtonMoreActions] not find");
                    return;
                }

                btGameSelectedActionBar.Height = btGameSelectedActionBarChild.ActualHeight;
                if (btGameSelectedActionBarChild.Name == "PART_ButtonMoreActions")
                {
                    btGameSelectedActionBar.Width = btGameSelectedActionBarChild.ActualWidth;
                }

                // Not find element
                if (btGameSelectedActionBarChild == null)
                {
                    logger.Error("PluginCommon - btGameSelectedActionBarChild [PART_ButtonMoreActions] not find");
                    return;
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
                    StackPanel spContener = (StackPanel)SearchElementByName("PART_spContener", true);

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

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBar.Name})");
                return;
            }
        }

        public void RemoveButtonInGameSelectedActionBarButtonOrToggleButton(string btGameSelectedActionBarName)
        {
            try
            {
                // Not find element
                if (btGameSelectedActionBarChild == null)
                {
                    logger.Error("PluginCommon - btGameSelectedActionBarChild [PART_ButtonMoreActions] not find");
                    return;
                }

                FrameworkElement spContener = SearchElementByName("PART_spContener", btGameSelectedActionBarChild.Parent, true);

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

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in RemoveButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBarName})");
                return;
            }
        }
        #endregion


        #region GameSelectedDescription
        private FrameworkElement elGameSelectedDescriptionContener = null;

        public void AddElementInGameSelectedDescription(FrameworkElement elGameSelectedDescription, bool isTop = false)
        {
            try
            {
                elGameSelectedDescriptionContener = SearchElementByName("PART_HtmlDescription", true);

                // Not find element
                if (elGameSelectedDescriptionContener == null)
                {
                    logger.Warn("PluginCommon - elGameSelectedDescriptionContener [PART_ElemDescription] not find");
                    return;
                }

                elGameSelectedDescriptionContener = (FrameworkElement)elGameSelectedDescriptionContener.Parent;

                // Not find element
                if (elGameSelectedDescriptionContener == null)
                {
                    logger.Warn("PluginCommon - elGameSelectedDescriptionContener.Parent [PART_ElemDescription] not find");
                    return;
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
                    return;
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
                    return;
                }

                logger.Warn($"PluginCommon - elGameSelectedDescriptionContener is {elGameSelectedDescriptionContener.ToString()}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddElementInGameSelectedDescription({elGameSelectedDescription.Name})");
                return;
            }
        }

        public void RemoveElementInGameSelectedDescription(string elGameSelectedDescriptionName)
        {
            try
            {
                // Not find element
                if (elGameSelectedDescriptionContener == null)
                {
                    logger.Error("PluginCommon - elGameSelectedDescriptionContener [PART_ElemDescription] not find");
                    return;
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
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in RemoveElementInGameSelectedDescription({elGameSelectedDescriptionName})");
                return;
            }
        }
        #endregion


        #region Custom theme
        private List<FrameworkElement> ListCustomElement = new List<FrameworkElement>();

        public void AddElementInCustomTheme(FrameworkElement ElementInCustomTheme, string ElementParentInCustomThemeName)
        {
            try
            {
                FrameworkElement ElementCustomTheme = SearchElementByName(ElementParentInCustomThemeName, false, true);

                // Not find element
                if (ElementCustomTheme == null)
                {
                    logger.Error($"PluginCommon - ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                    return;
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
                    ((StackPanel)ElementCustomTheme).UpdateLayout();

                    ListCustomElement.Add(ElementCustomTheme);
#if DEBUG
                    logger.Debug($"PluginCommon - ElementCustomTheme [{ElementCustomTheme.Name}] insert");
#endif
                }
                else
                {
                    logger.Error($"PluginCommon - ElementCustomTheme is not a StackPanel element");
                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddElementInCustomTheme({ElementParentInCustomThemeName})");
                return;
            }
        }

        public void ClearElementInCustomTheme(string ElementParentInCustomThemeName)
        {
            try
            {
                foreach (FrameworkElement ElementCustomTheme in ListCustomElement)
                {
                    // Not find element
                    if (ElementCustomTheme == null)
                    {
                        logger.Error($"PluginCommon - ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                    }

                    // Add in parent if good type
                    if (ElementCustomTheme is StackPanel)
                    {
                        // Clear FrameworkElement 
                        ((StackPanel)ElementCustomTheme).Children.Clear();
                        ((StackPanel)ElementCustomTheme).UpdateLayout();

#if DEBUG
                        logger.Debug($"PluginCommon - ElementCustomTheme [{ElementCustomTheme.Name}] clear");
#endif
                    }
                    else
                    {
                        logger.Error($"PluginCommon - ElementCustomTheme is not a StackPanel element");
                    }
                }

                 ListCustomElement = new List<FrameworkElement>();
                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in ClearElementInCustomTheme({ElementParentInCustomThemeName})");
                ListCustomElement = new List<FrameworkElement>();
                return;
            }
        }
        #endregion

        


        #region GameSelectedInfoBarFS
        private FrameworkElement spInfoBarFS = null;

        public void AddStackPanelInGameSelectedInfoBarFS(FrameworkElement spGameSelectedInfoBarFS)
        {
            try
            {
                FrameworkElement tempElement = SearchElementByName("PART_ButtonContext");
                if (tempElement != null)
                {
                    tempElement = (FrameworkElement)tempElement.Parent;
                    tempElement = (FrameworkElement)tempElement.Parent;

                    spInfoBarFS = Tools.FindVisualChildren<StackPanel>(tempElement).FirstOrDefault();

                    // Not find element
                    if (spInfoBarFS == null)
                    {
                        logger.Error("PluginCommon - btGameSelectedInfoBarFS Parent [PART_ButtonContext] not find");
                        return;
                    }

                    // Add element 
                    if (spInfoBarFS is StackPanel)
                    {
                        ((StackPanel)(spInfoBarFS)).Children.Add(spGameSelectedInfoBarFS);
                        ((StackPanel)(spInfoBarFS)).UpdateLayout();

#if DEBUG
                        logger.Debug($"PluginCommon - (StackPanel)btGameSelectedActionBarFS [{spGameSelectedInfoBarFS.Name}] insert");
#endif
                    }
                }
                else
                {
                    logger.Error("PluginCommon - btGameSelectedInfoBarFS [PART_ButtonContext] not find");
                    return;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon");
            }
        }

        public void RemoveStackPanelInGameSelectedInfoBarFS(string spGameSelectedInfoBarNameFS)
        {
            try
            {
                // Not find element
                if (spInfoBarFS == null)
                {
                    logger.Error("PluginCommon - btGameSelectedInfoBarFS [PART_ButtonContext] not find");
                    return;
                }

                // Remove in parent if good type
                if (spInfoBarFS is StackPanel)
                {
                    for (int i = 0; i < ((StackPanel)spInfoBarFS).Children.Count; i++)
                    {
                        if (((FrameworkElement)((StackPanel)spInfoBarFS).Children[i]).Name == spGameSelectedInfoBarNameFS)
                        {
                            ((StackPanel)spInfoBarFS).Children.Remove(((StackPanel)spInfoBarFS).Children[i]);
                            ((StackPanel)spInfoBarFS).UpdateLayout();

#if DEBUG
                            logger.Debug($"PluginCommon - (StackPanel)btGameSelectedInfoBarFS [{spGameSelectedInfoBarNameFS}] remove");
#endif
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon");
            }
        }
        #endregion


        #region GameSelectedActionBarFS
        private FrameworkElement btGameSelectedActionBarChildFS = null;

        public void AddButtonInGameSelectedActionBarButtonOrToggleButtonFS(FrameworkElement btGameSelectedActionBarFS)
        {
            try
            {
                btGameSelectedActionBarChildFS = SearchElementByName("PART_ButtonContext", true);

                // Not find element
                if (btGameSelectedActionBarChildFS == null)
                {
                    logger.Error("PluginCommon - btGameSelectedActionBarChildFS [PART_ButtonContext] not find");
                    return;
                }

                btGameSelectedActionBarFS.Height = btGameSelectedActionBarChildFS.ActualHeight;
                if (btGameSelectedActionBarFS.Name == "PART_ButtonContext")
                {
                    btGameSelectedActionBarFS.Width = btGameSelectedActionBarChildFS.ActualWidth;
                }

                // Not find element
                if (btGameSelectedActionBarChildFS == null)
                {
                    logger.Error("PluginCommon - btGameSelectedActionBarChildFS [PART_ButtonContext] not find");
                    return;
                }

                // Add in parent if good type
                if (btGameSelectedActionBarChildFS.Parent is StackPanel)
                {
                    // Add button 
                    ((StackPanel)(btGameSelectedActionBarChildFS.Parent)).Children.Add(btGameSelectedActionBarFS);
                    ((StackPanel)(btGameSelectedActionBarChildFS.Parent)).UpdateLayout();

#if DEBUG
                    logger.Debug($"PluginCommon - (StackPanel)btGameSelectedActionBarFS [{btGameSelectedActionBarFS.Name}] insert");
#endif
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddButtonInGameSelectedActionBarButtonOrToggleButtonFS({btGameSelectedActionBarFS.Name})");
                return;
            }
        }

        public void RemoveButtonInGameSelectedActionBarButtonOrToggleButtonFS(string btGameSelectedActionBarNameFS)
        {
            try
            {
                // Not find element
                if (btGameSelectedActionBarChildFS == null)
                {
                    logger.Error("PluginCommon - btGameSelectedActionBarChildFS [PART_ButtonContext] not find");
                    return;
                }

                // Remove in parent if good type
                if (btGameSelectedActionBarChildFS.Parent is StackPanel && btGameSelectedActionBarChildFS != null)
                {
                    StackPanel btGameSelectedParent = ((StackPanel)(btGameSelectedActionBarChildFS.Parent));
                    for (int i = 0; i < btGameSelectedParent.Children.Count; i++)
                    {
                        if (((FrameworkElement)btGameSelectedParent.Children[i]).Name == btGameSelectedActionBarNameFS)
                        {
                            btGameSelectedParent.Children.Remove(btGameSelectedParent.Children[i]);
                            btGameSelectedParent.UpdateLayout();

#if DEBUG
                            logger.Debug($"PluginCommon - (StackPanel)btGameSelectedActionBarChildFS [{btGameSelectedActionBarNameFS}] remove");
#endif
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in RemoveButtonInGameSelectedActionBarButtonOrToggleButton({btGameSelectedActionBarNameFS})");
                return;
            }
        }
        #endregion


        #region GameSelectedDescription
        private FrameworkElement elGameSelectedDescriptionContenerFS = null;

        public void AddElementInGameSelectedDescriptionFS(FrameworkElement elGameSelectedDescriptionFS)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in AddElementInGameSelectedDescriptionFS({elGameSelectedDescriptionFS.Name})");
                return;
            }
        }

        public void RemoveElementInGameSelectedDescriptionFS(string elGameSelectedDescriptionFSName)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error in RemoveElementInGameSelectedDescriptionFS({elGameSelectedDescriptionFSName})");
                return;
            }
        }
        #endregion




        public static FrameworkElement SearchElementByName(string ElementName, bool MustVisible = false, bool ParentMustVisible = false)
        {
            return SearchElementByName(ElementName, Application.Current.MainWindow, MustVisible, ParentMustVisible);
        }

        public static FrameworkElement SearchElementByName(string ElementName, DependencyObject dpObj, bool MustVisible = false, bool ParentMustVisible = false)
        {
            FrameworkElement ElementFind = null;

            if (ElementFind == null)
            {
                foreach (FrameworkElement el in Tools.FindVisualChildren<FrameworkElement>(dpObj))
                {
                    if (el.Name == ElementName)
                    {
                        if (!MustVisible)
                        {
                            if (!ParentMustVisible)
                            {
                                ElementFind = el;
                                break;
                            }
                            else if (((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)el.Parent).Parent).Parent).Parent).Parent).IsVisible)
                            {
                                ElementFind = el;
                                break;
                            }
                        }
                        else if (el.IsVisible)
                        {
                            if (!ParentMustVisible)
                            {
                                ElementFind = el;
                                break;
                            }
                            else if (((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)el.Parent).Parent).Parent).Parent).Parent).IsVisible)
                            {
                                ElementFind = el;
                                break;
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

#if DEBUG
                logger.Debug($"PluginCommon - SetControlSize({ControlElement.Name}) - parent.name: {ControlContener.Name} - parent.Height: {ControlContener.Height} - parent.Width: {ControlContener.Width} - parent.MaxHeight: {ControlContener.MaxHeight} - parent.MaxWidth: {ControlContener.MaxWidth}");
#endif

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
