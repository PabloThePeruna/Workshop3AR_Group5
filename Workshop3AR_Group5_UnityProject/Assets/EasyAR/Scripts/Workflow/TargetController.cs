//================================================================================================================================
//
//  Copyright (c) 2015-2021 VisionStar Information Technology (Shanghai) Co., Ltd. All Rights Reserved.
//  EasyAR is the registered trademark or trademark of VisionStar Information Technology (Shanghai) Co., Ltd in China
//  and other countries for the augmented reality technology developed by VisionStar Information Technology (Shanghai) Co., Ltd.
//
//================================================================================================================================

using System;
using UnityEngine;
using UnityEngine.UI;



namespace easyar
{
  
    public abstract class TargetController : MonoBehaviour
    {
        
        public ActiveControlStrategy ActiveControl;
        public bool HorizontalFlip;
        
        private bool firstFound; 
       
        public event Action TargetFound;
        public event Action TargetLost;

        public Canvas CanvasCuma;
        public Canvas CanvasScanner;
        public Canvas CanvasGold;
     

        public enum ActiveControlStrategy
        {
            
            HideWhenNotTracking,
            
            HideBeforeFirstFound,
            
            None,
        }

     
        public bool IsTracked { get; private set; }
       
        public bool IsLoaded { get; protected set; }

        
        protected virtual void Start()
        {
            if (!IsTracked && (ActiveControl == ActiveControlStrategy.HideWhenNotTracking || ActiveControl == ActiveControlStrategy.HideBeforeFirstFound))
            {
                ActivateRenderers(false);
            }
        }

        void ActivateRenderers(bool active)
        {
            foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
            {
                rend.enabled = active; 
            }
        }

       
        internal void OnTracking(bool status)
        {
            if (IsTracked != status)
            {
                if (status)
                {
                    if (ActiveControl == ActiveControlStrategy.HideWhenNotTracking || (ActiveControl == ActiveControlStrategy.HideBeforeFirstFound && !firstFound))
                    {
                        ActivateRenderers(true);

                        if (gameObject.tag == "Cuma")
                        {
                            CanvasCuma.gameObject.SetActive(true);
                            CanvasScanner.gameObject.SetActive(false);

                            Debug.Log("Cuma");

                        }

                        if (gameObject.tag == "Gold")
                        {
                            CanvasGold.gameObject.SetActive(true);
                            CanvasScanner.gameObject.SetActive(false);
                            Debug.Log("Gold");

                        }


                    }
                    firstFound = true;
                    if (TargetFound != null)
                    {
                        TargetFound();
                        Debug.Log("Katosi");

                    }
                }
                else
                {
                    if (ActiveControl == ActiveControlStrategy.HideWhenNotTracking)
                    {
                        ActivateRenderers(false);
                    }
                    if (TargetLost != null)
                    {
                        TargetLost();
                    }
                }
                IsTracked = status;
            }
            if (IsTracked)
            {
                OnTracking();
            }
        }

        
        protected abstract void OnTracking();
    }
}
