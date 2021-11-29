﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Trader20;
using UnityEngine;
using UnityEngine.UI;
using Patches = Trader20.Patches;
using Random = UnityEngine.Random;

public class OdinStore : MonoBehaviour
{
    private static OdinStore m_instance;
    
    [SerializeField] private GameObject? m_StorePanel;
    [SerializeField] private RectTransform? ListRoot;
    [SerializeField] private Text? SelectedItemDescription;
    [SerializeField] private Image? ItemDropIcon;
    [SerializeField] private Text? SelectedCost;
    [SerializeField] private Text? StoreTitle;
    [SerializeField] private Button? BuyButton;
    [SerializeField] private Text? SelectedName;

    [SerializeField] internal Image? Bkg1;
    [SerializeField] internal Image? Bkg2;
    
    
    //ElementData
    [SerializeField] private GameObject? ElementGO;

    [SerializeField] private NewTrader? _trader;
    [SerializeField] internal Image? ButtonImage;
    [SerializeField] internal Image? Coins;
    
    //StoreInventoryListing
    internal Dictionary<ItemDrop, KeyValuePair<int, KeyValuePair<int, int>>> _storeInventory = new();
    
    public static OdinStore instance => m_instance;
    internal static ElementFormat? tempElement;
    internal static Material? litpanel;
    internal List<GameObject> CurrentStoreList = new();
    internal List<ElementFormat> _elements = new();
    private void Awake() 
    {
        m_instance = this;
        m_StorePanel!.SetActive(false);
        StoreTitle!.text = "Knarr's Shop";
        try
        {
            Bkg1!.material = litpanel;
            Bkg2!.material = litpanel;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    private void Update()
    {
        if (!IsActive()) return;
        if (IsActive())
        {
            StoreGui.instance.m_hiddenFrames = 0;
        }
        if (Player.m_localPlayer is not Player player)
        {
            return;
        }
        if (Vector3.Distance(NewTrader.instance.transform.position, Player.m_localPlayer.transform.position) > 15)
        {
            Hide();
        }
        if ( Input.GetKeyDown(KeyCode.Escape))
        {
            ZInput.ResetButtonStatus("JoyButtonB");
            Hide();
        }
        if (InventoryGui.IsVisible() || Minimap.IsOpen())
        {
            Hide();
        }
        if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead() || Player.m_localPlayer.InCutscene())
        {
            Hide();
        }
    }

    private bool IsActive()
    {
        return m_StorePanel!.activeSelf;
    }
    private void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null!;
        }
    }

    private void  ClearStore()
   {
        if (CurrentStoreList.Count != _storeInventory.Count)
        {
            foreach (var go in CurrentStoreList)
            {
                Destroy(go);
            }
            
            CurrentStoreList.Clear();
            ReadItems();
            
        }
   }

    internal void ForceClearStore()
    {
        foreach (var go in CurrentStoreList)
        {
            Destroy(go);
        }
            
        CurrentStoreList.Clear();
        ReadItems();
    }

    /// <summary>
    /// This method is invoked to add an item to the visual display of the store, it expects the ItemDrop.ItemData and the stack as arguments
    /// </summary>
    /// <param name="drop"></param>
    /// <param name="stack"></param>
    /// <param name="cost"></param>
    public void AddItemToDisplayList(ItemDrop drop, int stack, int cost)
    {
        ElementFormat newElement = new();
        newElement.Drop = drop;
        newElement.Icon = drop.m_itemData.m_shared.m_icons.FirstOrDefault();
        newElement.ItemName = drop.m_itemData.m_shared.m_name;
        newElement.Drop.m_itemData.m_stack = stack;
        newElement.Element = ElementGO;
        
        
        newElement.Element!.transform.Find("icon").GetComponent<Image>().sprite = newElement.Icon;
        var component = newElement.Element.transform.Find("name").GetComponent<Text>();
        component.text = newElement.ItemName;
        component.gameObject.AddComponent<Localize>();
        
        newElement.Element.transform.Find("price").GetComponent<Text>().text = cost.ToString();
        newElement.Element.transform.Find("stack").GetComponent<Text>().text = stack switch
        {
            > 1 => "x" + stack,
            1 => "",
            _ => newElement.Element.transform.Find("stack").GetComponent<Text>().text
        };
        var elementthing = Instantiate(newElement.Element, ListRoot!.transform, false);
        elementthing.GetComponent<Button>().onClick.AddListener(delegate
        {
            UpdateGenDescription(newElement);
        });
        elementthing.transform.SetSiblingIndex(ListRoot.transform.GetSiblingIndex() - 1);
        elementthing.transform.Find("coin_bkg/coin icon").GetComponent<Image>().sprite = Trader20.Trader20.coins;
        _elements.Add(newElement);
        CurrentStoreList.Add(elementthing);
    }

    internal void  ReadItems()
    {
        foreach (var itemData in _storeInventory)
        {
            //need to add some type of second level logic here to think about if items exist do not repopulate.....
            AddItemToDisplayList(itemData.Key,itemData.Value.Value.Key, itemData.Value.Key);
        }
    }

    /// <summary>
    /// Invoke this method to instantiate an item from the storeInventory dictionary. This method expects an integer argument this integer should identify the index in the dictionary that the item lives at you wish to vend
    /// </summary>
    /// <param name="i"></param>
    public void SellItem(int i)
    {
        var inv = Player.m_localPlayer.GetInventory();
        var itemDrop = _storeInventory.ElementAt(i).Key;
        var tempcount = 0;
        tempcount-= _storeInventory.ElementAt(i).Value.Value.Key;
        
        itemDrop.m_itemData.m_dropPrefab = ZNetScene.instance.GetPrefab(itemDrop.gameObject.name);
        if (inv.AddItem(itemDrop.m_itemData)) return;
        //spawn item on ground if no inventory room
        var vector = Random.insideUnitSphere * 0.5f;
        var transform1 = Player.m_localPlayer.transform;
        Instantiate(_storeInventory.ElementAt(i).Key.gameObject,
            transform1.position + transform1.forward * 2f + Vector3.up + vector,
            Quaternion.identity);
        if (itemDrop == null || itemDrop.m_itemData == null) return;

        itemDrop.m_itemData.m_stack = _storeInventory.ElementAt(i).Value.Value.Key;
        itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
    }


    /// <summary>
    ///  Adds item to stores dictionary pass ItemDrop.ItemData and an integer for price
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <param name="price"></param>
    /// <param name="stack"></param>
    /// <param name="inv_count"></param>
    public void AddItemToDict(ItemDrop itemDrop, int price, int stack, int inv_count)
    {
        _storeInventory.Add(itemDrop, new KeyValuePair<int, KeyValuePair<int, int>>(price, new KeyValuePair<int, int>(stack, inv_count)) );

    }

    /// <summary>
    /// Pass this method an ItemDrop as an argument to drop it from the storeInventory dictionary.
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <returns>returns true if specific item is removed from trader inventory. Use this in tandem with inventory management</returns>
    public bool RemoveItemFromDict(ItemDrop itemDrop)
    {
        return _storeInventory.Remove(itemDrop);
    }

    /// <summary>
    /// This methods invocation should return the index offset of the ItemDrop passed as an argument, this is for use with other functions that expect an index to be passed as an integer argument
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <returns>returns index of item within trader inventory</returns>
    private int FindIndex(ItemDrop itemDrop)
    {
        var templist = _storeInventory.Keys.ToList();
        var index = templist.IndexOf(itemDrop);

        return index;

    }
    /// <summary>
    /// This method will update the general description of the store page pass it an ElementFormat as argument
    /// </summary>
    /// <param name="element"></param>
    public void UpdateGenDescription(ElementFormat element)
    {
        SelectedItemDescription!.text = element.Drop!.m_itemData.m_shared.m_description;
        SelectedItemDescription.gameObject.AddComponent<Localize>();
        ItemDropIcon!.sprite = element.Icon;
        tempElement = element;
    }

    /// <summary>
    /// Call this method to update the coins shown in UI with coins in player inventory
    /// </summary>
    public void UpdateCoins()
    {
        SelectedCost!.text = GetPlayerCoins().ToString();
    }
    
    /// <summary>
    /// Call this method upon attempting to buy something (this is tied to an onclick event)
    /// </summary>
    public void BuyButtonAction()
    {
        if (tempElement!.Drop is null) return;
        var i = FindIndex(tempElement.Drop);
        if (!CanBuy(i)) return;
        SellItem(i);
        NewTrader.instance.OnSold();
        SelectedCost!.text = GetPlayerCoins().ToString();
    }

    /// <summary>
    /// give this bool the index of your item within the traders inventory and it will return true/false based on players bank
    /// </summary>
    /// <param name="i"></param>
    /// <returns>return true/false based on players bank</returns>
    private bool CanBuy(int i)
    {
        var playerbank = GetPlayerCoins();
        var cost = _storeInventory.ElementAt(i).Value.Key;
        if (playerbank >= cost)
        {
            Player.m_localPlayer.GetInventory()
                .RemoveItem(ZNetScene.instance.GetPrefab(Trader20.Trader20.CurrencyPrefabName.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name,
                    cost);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Format of the Element GameObject that populates the for sale list.
    /// </summary>
    public class ElementFormat
    {
        internal GameObject? Element;
        internal Sprite? Icon;
        internal string? ItemName;
        internal int? Price;
        internal ItemDrop? Drop;
        internal int? InventoryCount;
    }
    
    /// <summary>
    /// Called to Hide the UI
    /// </summary>
    public void Hide()
    {
        m_StorePanel!.SetActive(false);
    }

    /// <summary>
    /// Called to show the UI
    /// </summary>
    public void Show()
    {
        m_StorePanel!.SetActive(true);
        ClearStore();
        if(_elements.Count >=1)
        {
            UpdateGenDescription(_elements[0]);
        }
        UpdateCoins();
    }

    
    /// <summary>
    /// Returns the players coin count as int
    /// </summary>
    /// <returns>Player Coin Count as int</returns>
    private static int GetPlayerCoins()
    {
        return Player.m_localPlayer.GetInventory().CountItems(ZNetScene.instance.GetPrefab(Trader20.Trader20.CurrencyPrefabName.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
    }

    public void DumpDict()
    {
        _storeInventory.Clear();
    }
}
