using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkanneriViivaLiike : MonoBehaviour
{
    public float nopeus = 100;
    public float maxSijainti = 65;
    public float minSijainti = -65;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        Vector2 position = rectTransform.anchoredPosition;

        position.x += nopeus * Time.deltaTime;

        if (position.x > maxSijainti)

            position.x = minSijainti;

        rectTransform.anchoredPosition = position;
    }
}