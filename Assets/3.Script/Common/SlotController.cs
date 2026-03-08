using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotController : MonoBehaviour
{
    [SerializeField] private Image plateImage;

    
    private void Awake()
    {
        if (plateImage == null)
        {
            Transform tran = transform.Find("Plate_UI");
            if (tran != null) plateImage = tran.GetComponent<Image>();
        }
    }

    public void RefreshColor(string tag = null)
    {
        if (plateImage != null)
        {
            plateImage.color = SlotColorUtility.GetColorByTag(tag);
        }
    }
}