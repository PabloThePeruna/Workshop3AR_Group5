using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class StartingMenu : MonoBehaviour
{

    public Button ScanButton;
    public Canvas CanvasMenu;
    public Canvas CanvasScanner;

    // Start is called before the first frame update
    void Start()
    {
        CanvasMenu.gameObject.SetActive(true);
        ScanButton = GetComponent<Button>();
        ScanButton.onClick.AddListener(StartToScan);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartToScan()
    {
        CanvasMenu.gameObject.SetActive(false);
        CanvasScanner.gameObject.SetActive(true);
    }
}