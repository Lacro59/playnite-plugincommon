using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
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
                    // Add search element if not allready find
                    if (btGameSelectedActionBarChild == null)
                    {
                        btGameSelectedActionBarChild = SearchElementByName("PART_ButtonEditGame");
                    }

                    // Not find element
                    if (btGameSelectedActionBarChild == null)
                    {
                        throw new Exception("btGameSelectedActionBarChild [PART_ButtonEditGame] not find");
                    }

                    // Add in parent if good type
                    if (btGameSelectedActionBarChild.Parent is StackPanel)
                    {
                        btGameSelectedActionBar.Height = btGameSelectedActionBarChild.ActualHeight;

                        // Add button 
                        ((StackPanel)(btGameSelectedActionBarChild.Parent)).Children.Add(btGameSelectedActionBar);
                        ((StackPanel)(btGameSelectedActionBarChild.Parent)).UpdateLayout();
                        logger.Info($"PluginCommon - btGameSelectedActionBar [{btGameSelectedActionBar.Name}] insert");
                    }
                    else
                    {
                        throw new Exception("btGameSelectedActionBarChild.Parent is not a StackPanel element");
                    }
                }
                else
                {
                    logger.Info($"PluginCommon - btGameSelectedActionBar [{btGameSelectedActionBar.Name}] allready insert");
                }

                btGameSelectedActionBarList.Add(btGameSelectedActionBar);
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
                // Remove only if not exist
                if (SearchElementInsert(btGameSelectedActionBarList, btGameSelectedActionBarName))
                {
                    // Add search element if not allready find
                    if (btGameSelectedActionBarChild == null)
                    {
                        btGameSelectedActionBarChild = SearchElementByName("PART_ButtonEditGame");
                    }

                    // Not find element
                    if (btGameSelectedActionBarChild == null)
                    {
                        throw new Exception("btGameSelectedActionBarChild [PART_ButtonEditGame] not find");
                    }

                    // Remove in parent if good type
                    if (btGameSelectedActionBarChild.Parent is StackPanel)
                    {
                        StackPanel btGameSelectedParent = ((StackPanel)(btGameSelectedActionBarChild.Parent));
                        for (int i = 0; i < btGameSelectedParent.Children.Count; i++)
                        {
                            if (((FrameworkElement)btGameSelectedParent.Children[i]).Name == btGameSelectedActionBarName)
                            {
                                btGameSelectedParent.Children.Remove(btGameSelectedParent.Children[i]);
                                btGameSelectedParent.UpdateLayout();
                                logger.Info($"PluginCommon - btGameSelectedActionBar [{btGameSelectedActionBarName}] remove");

                                foreach (FrameworkElement el in btGameSelectedActionBarList)
                                {
                                    if (btGameSelectedActionBarName == el.Name)
                                    {
                                        btGameSelectedActionBarList.Remove(el);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("btGameSelectedActionBarChild.Parent is not a StackPanel element");
                    }
                }
                else
                {
                    logger.Info($"PluginCommon - btGameSelectedActionBarName [{btGameSelectedActionBarName}] allready remove");
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
                    // Add search element if not allready find
                    if (elGameSelectedDescriptionContener == null)
                    {
                        elGameSelectedDescriptionContener = SearchElementByName("PART_ElemDescription");
                    }

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
                    else
                    {
                        throw new Exception("elGameSelectedDescriptionContener is not a StackPanel element");
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
                    if (elGameSelectedDescriptionContener == null)
                    {
                        elGameSelectedDescriptionContener = SearchElementByName("PART_ButtonEditGame");
                    }

                    // Not find element
                    if (elGameSelectedDescriptionContener == null)
                    {
                        throw new Exception("elGameSelectedDescriptionContener [PART_ButtonEditGame] not find");
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
                    else
                    {
                        throw new Exception("elGameSelectedDescriptionContener is not a StackPanel element");
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
        private ConcurrentDictionary<string, FrameworkElement> elCustomTheme = new ConcurrentDictionary<string, FrameworkElement>();
        public bool AddElementInCustomTheme(FrameworkElement ElementInCustomTheme, string ElementParentInCustomThemeName)
        {
            try
            {
                // Get or set element
                FrameworkElement ElementCustomTheme = elCustomTheme.GetOrAdd(ElementParentInCustomThemeName, SearchElementByName(ElementParentInCustomThemeName));

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
                // Get or set element
                FrameworkElement ElementCustomTheme = elCustomTheme.GetOrAdd(ElementParentInCustomThemeName, SearchElementByName(ElementParentInCustomThemeName));

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
                Common.LogError(ex, "PluginCommon", $"Error in ClearElementInCustomTheme({ElementParentInCustomThemeName})");
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

            logger.Debug(JsonConvert.SerializeObject(ElementFind.ToString()));

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
                if (ElSearch == el)
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
