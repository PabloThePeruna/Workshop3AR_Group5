﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrlOpener : MonoBehaviour
{
    public string Url;

    // Start is called before the first frame update
    public void OpenURL()
    {
        Application.OpenURL(Url);
        Debug.Log("is this working");
    }

}
