using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Playnite.SDK;

namespace PluginCommon
{
    public class IntegrationUI
    {
        private static ILogger logger = LogManager.GetLogger();


        public bool AddResources(List<ResourcesList> ResourcesList)
        {
            try
            {
                foreach (ResourcesList item in ResourcesList)
                {
                    Application.Current.Resources.Remove(item.Key);
                    Application.Current.Resources.Add(item.Key, item.Value);
                }
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error in AddResources()");
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
                        throw new Exception("btHeaderChild [PART_ButtonSteamFriends] not find");
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
                                logger.Info($"PluginCommon - btHeader [{btHeader.Name}] insert");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("btHeaderChild.Parent is not a DockPanel element");
                    }
                }
                else
                {
                    logger.Info($"PluginCommon - btHeader [{btHeader.Name}] allready insert");
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

                    btGameSelectedActionBar.Height = btGameSelectedActionBarChild.ActualHeight;
                    if (btGameSelectedActionBar.Name == "PART_HltbButton")
                    {
                        btGameSelectedActionBar.Width = btGameSelectedActionBarChild.ActualWidth;
                    }

                    // Not find element
                    if (btGameSelectedActionBarChild == null)
                    {
                        throw new Exception("btGameSelectedActionBarChild [PART_ButtonEditGame] not find");
                    }

                    // Add in parent if good type
                    if (btGameSelectedActionBarChild.Parent is StackPanel)
                    {
                        // Add button 
                        ((StackPanel)(btGameSelectedActionBarChild.Parent)).Children.Add(btGameSelectedActionBar);
                        ((StackPanel)(btGameSelectedActionBarChild.Parent)).UpdateLayout();

                        logger.Info($"PluginCommon - (StackPanel)btGameSelectedActionBar [{btGameSelectedActionBar.Name}] insert");
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

                        logger.Info($"PluginCommon - (Grid)btGameSelectedActionBar [{btGameSelectedActionBar.Name}] insert");
                    }
                }
                else
                {
                    logger.Info($"PluginCommon - btGameSelectedActionBar [{btGameSelectedActionBar.Name}] allready insert");
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
                foreach (var tt in btGameSelectedActionBarList)
                {
                    logger.Debug(btGameSelectedActionBarName + " " + tt.Name);
                }

                btGameSelectedActionBarChild = SearchElementByName("PART_ButtonEditGame");
                FrameworkElement spContener = SearchElementByName("PART_spContener");

                // Not find element
                if (btGameSelectedActionBarChild == null)
                {
                    throw new Exception("btGameSelectedActionBarChild [PART_ButtonEditGame] not find");
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

                            logger.Info($"PluginCommon - (StackPanel)btGameSelectedActionBar [{btGameSelectedActionBarName}] remove");
                        }
                    }
                }

                if (btGameSelectedActionBarChild.Parent is Grid && spContener != null)
                {
                    //Grid btGameSelectedParent = ((Grid)(btGameSelectedActionBarChild.Parent));
                    for (int i = 0; i < ((StackPanel)spContener).Children.Count; i++)
                    {
                        logger.Debug(((FrameworkElement)((StackPanel)spContener).Children[i]).Name);

                        if (((FrameworkElement)((StackPanel)spContener).Children[i]).Name == btGameSelectedActionBarName)
                        {
                            logger.Debug(btGameSelectedActionBarName + " " + ((Grid)(btGameSelectedActionBarChild.Parent)).ColumnDefinitions.Count);

                            ((StackPanel)spContener).Children.Remove(((StackPanel)spContener).Children[i]);
                            spContener.UpdateLayout();

                            logger.Info($"PluginCommon - (Grid)btGameSelectedActionBar [{btGameSelectedActionBarName}] remove");
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
                        throw new Exception("elGameSelectedDescriptionContener [PART_ElemDescription] not find");
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
                        logger.Info($"PluginCommon - elGameSelectedDescriptionContener [{elGameSelectedDescription.Name}] insert");
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
                        logger.Info($"PluginCommon - elGameSelectedDescriptionContener [{elGameSelectedDescription.Name}] insert");
                    }
                }
                else
                {
                    logger.Info($"PluginCommon - elGameSelectedDescription [{elGameSelectedDescription.Name}] allready insert");
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
                    // Add search element if not allready find
                    //if (elGameSelectedDescriptionContener == null)
                    //{
                    elGameSelectedDescriptionContener = SearchElementByName("PART_HtmlDescription");
                    elGameSelectedDescriptionContener = (FrameworkElement)elGameSelectedDescriptionContener.Parent;
                    //}

                    // Not find element
                    if (elGameSelectedDescriptionContener == null)
                    {
                        throw new Exception("elGameSelectedDescriptionContener [PART_ElemDescription] not find");
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
                                logger.Info($"PluginCommon - elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] remove");

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
                                logger.Info($"PluginCommon - elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] remove");

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
                    logger.Info($"PluginCommon - elGameSelectedDescriptionName [{elGameSelectedDescriptionName}] allready remove");
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
                    throw new Exception($"ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                }

                // Add in parent if good type
                if (ElementCustomTheme is StackPanel)
                {
                    // Add FrameworkElement 
                    ((StackPanel)ElementCustomTheme).Children.Add(ElementInCustomTheme);
                    ((StackPanel)ElementCustomTheme).Visibility = Visibility.Visible;
                    ((StackPanel)ElementCustomTheme).UpdateLayout();
                    logger.Info($"PluginCommon - ElementCustomTheme [{ElementCustomTheme.Name}] insert");
                }
                else
                {
                    throw new Exception("ElementCustomTheme is not a StackPanel element");
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
                    throw new Exception($"ElementCustomTheme [{ElementParentInCustomThemeName}] not find");
                }

                // Add in parent if good type
                if (ElementCustomTheme is StackPanel)
                {
                    // Clear FrameworkElement 
                    ((StackPanel)ElementCustomTheme).Children.Clear();
                    ((StackPanel)ElementCustomTheme).Visibility = Visibility.Collapsed;
                    ((StackPanel)ElementCustomTheme).UpdateLayout();
                    logger.Info($"PluginCommon - ElementCustomTheme [{ElementCustomTheme.Name}] clear");
                }
                else
                {
                    throw new Exception("ElementCustomTheme is not a StackPanel element");
                }

                return true;
            }
            catch (Exception ex)
            {
                //Common.LogError(ex, "PluginCommon", $"Error in ClearElementInCustomTheme({ElementParentInCustomThemeName})");
                return false;
            }
        }
        #endregion


        private FrameworkElement SearchElementByName(string ElementName)
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

        private bool SearchElementInsert(List<FrameworkElement> SearchList, string ElSearchName)
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
        private bool SearchElementInsert(List<FrameworkElement> SearchList, FrameworkElement ElSearch)
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
        public string Value { get; set; }
    }
}
