#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Represents a control that can be used to present a collection of items.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemscontrol.aspx
  /// </summary>
  public abstract class ItemsControl : Control
  {
    #region Protected fields

    protected AbstractProperty _selectionChangedProperty;
    protected AbstractProperty _itemsSourceProperty;
    protected AbstractProperty _itemTemplateProperty;
    protected AbstractProperty _itemContainerStyleProperty;
    protected AbstractProperty _itemsPanelProperty;
    protected AbstractProperty _dataStringProviderProperty;
    protected AbstractProperty _currentItemProperty;
    protected AbstractProperty _itemsProperty;
    protected AbstractProperty _isEmptyProperty;

    protected bool _templateApplied = false;
    protected Panel _itemsHostPanel = null;
    protected FrameworkElement _lastFocusedElement = null;

    #endregion

    #region Ctor

    protected ItemsControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _itemsSourceProperty = new SProperty(typeof(IEnumerable), null);
      _itemsProperty = new SProperty(typeof(ObservableUIElementCollection<FrameworkElement>), new ObservableUIElementCollection<FrameworkElement>(this));
      _itemTemplateProperty = new SProperty(typeof(DataTemplate), null);
      _itemContainerStyleProperty = new SProperty(typeof(Style), null);
      _itemsPanelProperty = new SProperty(typeof(ItemsPanelTemplate), null);
      _dataStringProviderProperty = new SProperty(typeof(DataStringProvider), null);
      _currentItemProperty = new SProperty(typeof(object), null);
      _selectionChangedProperty = new SProperty(typeof(ICommandStencil), null);
      _isEmptyProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _itemsSourceProperty.Attach(OnItemsSourceChanged);
      _itemsProperty.Attach(OnItemsChanged);
      _itemTemplateProperty.Attach(OnItemTemplateChanged);
      _itemsPanelProperty.Attach(OnItemsPanelChanged);
      _dataStringProviderProperty.Attach(OnDataStringProviderChanged);
      _itemContainerStyleProperty.Attach(OnItemContainerStyleChanged);
    }

    void Detach()
    {
      _itemsSourceProperty.Detach(OnItemsSourceChanged);
      _itemsProperty.Detach(OnItemsChanged);
      _itemTemplateProperty.Detach(OnItemTemplateChanged);
      _itemsPanelProperty.Detach(OnItemsPanelChanged);
      _dataStringProviderProperty.Attach(OnDataStringProviderChanged);
      _itemContainerStyleProperty.Detach(OnItemContainerStyleChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ItemsControl c = (ItemsControl) source;
      object oldItemsSource = ItemsSource;
      object oldItems = Items;
      ItemsSource = copyManager.GetCopy(c.ItemsSource);
      ItemContainerStyle = copyManager.GetCopy(c.ItemContainerStyle);
      SelectionChanged = copyManager.GetCopy(c.SelectionChanged);
      ItemTemplate = copyManager.GetCopy(c.ItemTemplate);
      ItemsPanel = copyManager.GetCopy(c.ItemsPanel);
      DataStringProvider = copyManager.GetCopy(c.DataStringProvider);
      Attach();
      OnItemsSourceChanged(_itemsSourceProperty, oldItemsSource);
      OnItemsChanged(_itemsProperty, oldItems);
      InvalidateItems();
    }

    public override void Dispose()
    {
      Detach();
      DetachFromItemsSource(ItemsSource);
      ObservableUIElementCollection<FrameworkElement> items = Items;
      if (items != null)
      {
        DetachFromItems(items);
        // Normally, the disposal of items will be done by our items host panel. But in the rare case that we didn't add
        // the Items to our host panel's Children yet, we need to clean up them manually.
        items.Dispose();
      }
      Registration.TryCleanupAndDispose(ItemTemplate);
      Registration.TryCleanupAndDispose(ItemContainerStyle);
      Registration.TryCleanupAndDispose(ItemsPanel);
      Registration.TryCleanupAndDispose(SelectionChanged);
      base.Dispose();
    }

    #endregion

    #region Event handlers

    protected void DetachFromItemsSource(IEnumerable itemsSource)
    {
      IObservable oldItemsSource = itemsSource as IObservable;
      if (oldItemsSource != null)
        oldItemsSource.ObjectChanged -= OnItemsSourceCollectionChanged;
    }

    protected void AttachToItemsSource(IEnumerable itemsSource)
    {
      IObservable coll = itemsSource as IObservable;
      if (coll != null)
        coll.ObjectChanged += OnItemsSourceCollectionChanged;
    }

    protected void DetachFromItems(ObservableUIElementCollection<FrameworkElement> items)
    {
      if (items == null)
        return;
      items.CollectionChanged -= OnItemsCollectionChanged;
    }

    protected void AttachToItems(ObservableUIElementCollection<FrameworkElement> items)
    {
      if (items == null)
        return;
      items.CollectionChanged += OnItemsCollectionChanged;
    }

    void OnItemsSourceChanged(AbstractProperty property, object oldValue)
    {
      DetachFromItemsSource(oldValue as IEnumerable);
      AttachToItemsSource(ItemsSource);
      OnItemsSourceChanged();
    }

    void OnItemsSourceCollectionChanged(IObservable itemsSource)
    {
      OnItemsSourceChanged();
    }

    void OnItemTemplateChanged(AbstractProperty property, object oldValue)
    {
      InvalidateItems();
    }

    void OnItemsPanelChanged(AbstractProperty property, object oldValue)
    {
      _templateApplied = false;
      InvalidateItems();
    }

    void OnDataStringProviderChanged(AbstractProperty property, object oldValue)
    {
      InvalidateItems();
    }

    void OnItemContainerStyleChanged(AbstractProperty property, object oldValue)
    {
      InvalidateItems();
    }

    void OnItemsChanged(AbstractProperty prop, object oldVal)
    {
      ObservableUIElementCollection<FrameworkElement> oldItems = oldVal as ObservableUIElementCollection<FrameworkElement>;
      if (oldItems != null)
      {
        DetachFromItems(oldItems);
        oldItems.Dispose();
      }
      ObservableUIElementCollection<FrameworkElement> items = Items;
      AttachToItems(items);
      items.SetParent(this);
      OnItemsChanged();
    }

    void OnItemsCollectionChanged(ObservableUIElementCollection<FrameworkElement> collection)
    {
      OnItemsChanged();
    }

    /// <summary>
    /// Will be called when the <see cref="ItemsSource"/> object or the <see cref="ItemsSource"/> collection were changed.
    /// </summary>
    protected virtual void OnItemsSourceChanged()
    {
      InvalidateItems();
    }

    /// <summary>
    /// Will be called when the <see cref="Items"/> object or the <see cref="Items"/> collection were changed.
    /// </summary>
    protected virtual void OnItemsChanged()
    {
      if (_itemsHostPanel == null)
        return;
      ObservableUIElementCollection<FrameworkElement> items = Items;
      FrameworkElementCollection children = _itemsHostPanel.Children;
      lock (children.SyncRoot)
      {
        children.Clear();
        if (items != null)
          children.AddAll(items);
        IsEmpty = children.Count == 0;
      }
    }

    #endregion

    #region Events

    public AbstractProperty SelectionChangedProperty
    {
      get { return _selectionChangedProperty; }
    }

    public ICommandStencil SelectionChanged
    {
      get { return (ICommandStencil)_selectionChangedProperty.GetValue(); }
      set { _selectionChangedProperty.SetValue(value); }
    }

    #endregion

    #region Public properties

    public AbstractProperty ItemsPanelProperty
    {
      get { return _itemsPanelProperty; }
    }

    /// <summary>
    /// Gets or sets the template that defines the panel that controls the layout of items.
    /// </summary>
    public ItemsPanelTemplate ItemsPanel
    {
      get { return (ItemsPanelTemplate) _itemsPanelProperty.GetValue(); }
      set { _itemsPanelProperty.SetValue(value); }
    }

    public AbstractProperty ItemsSourceProperty
    {
      get { return _itemsSourceProperty; }
    }

    /// <summary>
    /// Gets or sets an enumeration used to generate the content of the ItemsControl.
    /// </summary>
    public IEnumerable ItemsSource
    {
      get { return (IEnumerable) _itemsSourceProperty.GetValue(); }
      set { _itemsSourceProperty.SetValue(value); }
    }

    public AbstractProperty ItemContainerStyleProperty
    {
      get { return _itemContainerStyleProperty; }
    }

    public AbstractProperty ItemsProperty
    {
      get { return _itemsProperty; }
    }

    /// <summary>
    /// Gets or sets the items of the ItemsControl directly.
    /// </summary>
    public ObservableUIElementCollection<FrameworkElement> Items
    {
      get { return (ObservableUIElementCollection<FrameworkElement>) _itemsProperty.GetValue(); }
      set { _itemsProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the Style that is applied to the container element generated for each item.
    /// </summary>
    public Style ItemContainerStyle
    {
      get { return (Style) _itemContainerStyleProperty.GetValue(); }
      set { _itemContainerStyleProperty.SetValue(value); }
    }

    public AbstractProperty ItemTemplateProperty
    {
      get { return _itemTemplateProperty; }
    }

    /// <summary>
    /// Gets or sets the data template used to display each item.
    /// </summary>
    public DataTemplate ItemTemplate
    {
      get { return (DataTemplate) _itemTemplateProperty.GetValue(); }
      set { _itemTemplateProperty.SetValue(value); }
    }

    public AbstractProperty DataStringProviderProperty
    {
      get { return _dataStringProviderProperty; }
    }

    /// <summary>
    /// Gets or sets the data string provider which is used to build strings for each item to be able to
    /// focus items when the user types keys.
    /// </summary>
    public DataStringProvider DataStringProvider
    {
      get { return (DataStringProvider) _dataStringProviderProperty.GetValue(); }
      set { _dataStringProviderProperty.SetValue(value); }
    }

    public AbstractProperty CurrentItemProperty
    {
      get { return _currentItemProperty; }
    }

    public object CurrentItem
    {
      get { return _currentItemProperty.GetValue(); }
      internal set { _currentItemProperty.SetValue(value); }
    }

    public AbstractProperty IsEmptyProperty
    {
      get { return _isEmptyProperty; }
    }

    public bool IsEmpty
    {
      get { return (bool) _isEmptyProperty.GetValue(); }
      set { _isEmptyProperty.SetValue(value); }
    }

    public bool IsItemsPrepared
    {
      get { return _itemsHostPanel != null; }
    }

    #endregion

    #region Item management

    /// <summary>
    /// Checks if the currently focused element is contained in this items control.
    /// </summary>
    /// <param name="focusedElement">Currelty focused element.</param>
    bool CheckFocusInScope(FrameworkElement focusedElement)
    {
      Visual focusPath = focusedElement;
      while (focusPath != null)
      {
        if (focusPath == this)
          // Focused control is located in our focus scope
          return true;
        if (focusPath is ItemsControl)
          // Focused control is located in another itemscontrol's focus scope
          return false;
        focusPath = focusPath.VisualParent;
      }
      return false;
    }

    /// <summary>
    /// Will update the <see cref="CurrentItem"/> property. This method will be called when the
    /// current item might have changed.
    /// </summary>
    protected void UpdateCurrentItem()
    {
      Screen screen = Screen;
      _lastFocusedElement = screen == null ? null : screen.FocusedElement;
      if (_itemsHostPanel == null || !CheckFocusInScope(_lastFocusedElement))
        CurrentItem = null;
      else
      {
        Visual element = _lastFocusedElement;
        while (element != null && element.VisualParent != _itemsHostPanel)
          element = element.VisualParent;
        CurrentItem = element == null ? null : element.Context;
      }
      if (SelectionChanged != null)
        SelectionChanged.Execute(new object[] { CurrentItem });
    }

    protected virtual void InvalidateItems()
    {
      PrepareItems();
    }

    protected ItemsPresenter FindItemsPresenter()
    {
      FrameworkElement templateControl = TemplateControl;
      return templateControl == null ? null : templateControl.FindElement(
          new TypeMatcher(typeof(ItemsPresenter))) as ItemsPresenter;
    }

    protected IList<string> BuildDataStrings(IList<object> objects)
    {
      DataStringProvider dataStringProvider = DataStringProvider;
      if (dataStringProvider == null)
        return null;
      IList<string> result = new List<string>(objects.Count);
      foreach (object o in objects)
        result.Add(dataStringProvider.GenerateDataString(o));
      return result;
    }

    protected virtual void PrepareItems()
    {
      if (ItemsSource == null) return;
      if (ItemsPanel == null) return;
      if (TemplateControl == null) return;
      if (ItemContainerStyle == null) return;
      if (ItemTemplate == null) return;
      IList<object> l = new List<object>();
      ISynchronizable sync = ItemsSource as ISynchronizable;
      if (sync != null)
        lock (sync.SyncRoot)
          CollectionUtils.AddAll(l, ItemsSource);
      else
        CollectionUtils.AddAll(l, ItemsSource);
      IEnumerator enumer = l.GetEnumerator();
      ItemsPresenter presenter = FindItemsPresenter();
      if (presenter == null)
        return;

      if (!_templateApplied)
      {
        _templateApplied = true;
        presenter.ApplyTemplate(ItemsPanel);
        _itemsHostPanel = null;
      }

      presenter.SetDataStrings(BuildDataStrings(l));

      if (_itemsHostPanel == null)
        _itemsHostPanel = presenter.ItemsHostPanel;
      if (_itemsHostPanel == null)
        return;

      VirtualizingStackPanel vsp = _itemsHostPanel as VirtualizingStackPanel;
      if (vsp != null)
      {
        ListViewItemGenerator lvig = new ListViewItemGenerator();
        lvig.Initialize(this, l, ItemContainerStyle, ItemTemplate);
        SetValueInRenderThread(new SimplePropertyDataDescriptor(this, typeof(ItemsControl).GetProperty("IsEmpty")), l.Count == 0);
        SetValueInRenderThread(new SimplePropertyDataDescriptor(vsp, typeof(VirtualizingStackPanel).GetProperty("ItemProvider")), lvig);
      }
      else
      {
        ObservableUIElementCollection<FrameworkElement> items = new ObservableUIElementCollection<FrameworkElement>(null);
        while (enumer.MoveNext())
        {
          FrameworkElement container = PrepareItemContainer(enumer.Current);
          items.Add(container);
        }
        SetValueInRenderThread(new SimplePropertyDataDescriptor(this, typeof(ItemsControl).GetProperty("Items")), items);
      }
    }

    /// <summary>
    /// Creates an UI element which displays one of the items of this <see cref="ItemsControl"/>.
    /// The specified <paramref name="dataItem"/> is one of the items from the <see cref="ItemsSource"/>
    /// collection.
    /// </summary>
    /// <remarks>
    /// The implementor should use the <see cref="ItemContainerStyle"/> as style for the new container,
    /// and it should use the <see cref="ItemTemplate"/> as data template to display the
    /// <paramref name="dataItem"/>.
    /// </remarks>
    /// <param name="dataItem">Item to build a visible container for.</param>
    /// <returns>UI element which renders the specified <paramref name="dataItem"/>.</returns>
    protected abstract FrameworkElement PrepareItemContainer(object dataItem);

    public void SetFocusOnItem(object dataItem)
    {
      if (_itemsHostPanel == null || Screen == null)
        return;
      FrameworkElement item = null;
      lock (_itemsHostPanel.Children.SyncRoot)
        foreach (FrameworkElement child in _itemsHostPanel.Children)
          if (child.DataContext == dataItem)
            item = child;
      if (item == null)
        return;
      FrameworkElement focusable = Screen.FindFirstFocusableElement(item);
      if (focusable != null)
        focusable.TrySetFocus(true);
    }

    #endregion

    public override void DoRender(Rendering.RenderContext localRenderContext)
    {
      Screen screen = Screen;
      if (screen != null && _lastFocusedElement != screen.FocusedElement)
        UpdateCurrentItem();
      base.DoRender(localRenderContext);
    }
  }
}
