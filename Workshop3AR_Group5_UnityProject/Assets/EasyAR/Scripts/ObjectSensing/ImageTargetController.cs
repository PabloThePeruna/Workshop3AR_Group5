//================================================================================================================================
//
//  Copyright (c) 2015-2021 VisionStar Information Technology (Shanghai) Co., Ltd. All Rights Reserved.
//  EasyAR is the registered trademark or trademark of VisionStar Information Technology (Shanghai) Co., Ltd in China
//  and other countries for the augmented reality technology developed by VisionStar Information Technology (Shanghai) Co., Ltd.
//
//================================================================================================================================

using System;
using System.Collections;
using UnityEngine;

namespace easyar
{
  
    public class ImageTargetController : TargetController
    {
        
        public ImageTarget Target { get; private set; }

        
        public DataSource SourceType = DataSource.ImageFile;
        
        [HideInInspector, SerializeField]
        public ImageFileSourceData ImageFileSource = new ImageFileSourceData();
        
        [HideInInspector, SerializeField]
        public TargetDataFileSourceData TargetDataFileSource = new TargetDataFileSourceData();
        
        public ImageTarget TargetSource;

#if UNITY_EDITOR
        
        public GizmoStorage GizmoData = new GizmoStorage();
#endif

        [HideInInspector, SerializeField]
        private bool trackerHasSet;
        [HideInInspector, SerializeField]
        private ImageTrackerFrameFilter tracker;
        private ImageTrackerFrameFilter loader;
        private float scale = 0.1f;
        private float scaleX = 0.1f;
        private bool preHFlip;

        
        public event Action TargetAvailable;
        
        public event Action<Target, bool> TargetLoad;
       
        public event Action<Target, bool> TargetUnload;

        
        public enum DataSource
        {
           
            ImageFile,
           
            TargetDataFile,
          
            Target,
        }

        
        public ImageTrackerFrameFilter Tracker
        {
            get
            {
                return tracker;
            }
            set
            {
                tracker = value;
                UpdateTargetInTracker();
            }
        }

        
        public Vector2 Size
        {
            get
            {
                if (Target == null)
                {
                    return new Vector2();
                }
                return new Vector2(Target.scale(), Target.scale() / Target.aspectRatio());
            }
            private set { }
        }

        /// <summary>
        /// MonoBehaviour Start
        /// </summary>
        protected override void Start()
        {
            base.Start();
            if (!EasyARController.Initialized)
            {
                return;
            }

            switch (SourceType)
            {
                case DataSource.ImageFile:
                    LoadImageFile(new ImageFileSourceData()
                    {
                        PathType = ImageFileSource.PathType,
                        Path = ImageFileSource.Path,
                        Name = ImageFileSource.Name,
                        Scale = ImageFileSource.Scale
                    });
                    break;
                case DataSource.TargetDataFile:
                    LoadTargetDataFile(new TargetDataFileSourceData()
                    {
                        PathType = TargetDataFileSource.PathType,
                        Path = TargetDataFileSource.Path
                    });
                    break;
                case DataSource.Target:
                    LoadTarget(TargetSource);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// MonoBehaviour Update
        /// </summary>
        protected virtual void Update()
        {
            CheckScale();
        }

        /// <summary>
        /// MonoBehaviour OnDestroy
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (tracker)
            {
                tracker = null;
                UpdateTargetInTracker();
            }
            if (Target != null)
            {
                Target.Dispose();
                Target = null;
            }
        }

        protected override void OnTracking()
        {
            CheckScale();
        }

        private void LoadImageFile(ImageFileSourceData source)
        {
            EasyARController.Instance.StartCoroutine(FileUtil.LoadFile(source.Path, source.PathType, (Buffer buffer) =>
            {
                EasyARController.Instance.StartCoroutine(LoadImageBuffer(buffer.Clone(), source));
            }));
        }

        private void LoadTargetDataFile(TargetDataFileSourceData source)
        {
            EasyARController.Instance.StartCoroutine(FileUtil.LoadFile(source.Path, source.PathType, (Buffer buffer) =>
            {
                EasyARController.Instance.StartCoroutine(LoadTargetDataBuffer(buffer.Clone()));
            }));
        }

        private void LoadTarget(ImageTarget source)
        {
            Target = source;
            if (Target != null && TargetAvailable != null)
            {
                TargetAvailable();
            }
            UpdateScale();
            UpdateTargetInTracker();
        }

        private IEnumerator LoadImageBuffer(Buffer buffer, ImageFileSourceData source)
        {
            using (buffer)
            {
                Optional<Image> imageOptional = null;
                bool taskFinished = false;
                EasyARController.Instance.Worker.Run(() =>
                {
                    imageOptional = ImageHelper.decode(buffer);
                    taskFinished = true;
                });

                while (!taskFinished)
                {
                    yield return 0;
                }
                if (imageOptional.OnNone)
                {
                    throw new Exception("invalid buffer");
                }

                using (var image = imageOptional.Value)
                using (var param = new ImageTargetParameters())
                {
                    param.setImage(image);
                    param.setName(source.Name);
                    param.setScale(source.Scale);
                    param.setUid(Guid.NewGuid().ToString());
                    param.setMeta(string.Empty);
                    var targetOptional = ImageTarget.createFromParameters(param);
                    if (targetOptional.OnSome)
                    {
                        Target = targetOptional.Value;
                        if (Target != null && TargetAvailable != null)
                        {
                            TargetAvailable();
                        }
                    }
                    else
                    {
                        throw new Exception("invalid parameter");
                    }
                }
            }
            UpdateTargetInTracker();
        }

        private IEnumerator LoadTargetDataBuffer(Buffer buffer)
        {
            using (buffer)
            {
                Optional<ImageTarget> targetOptional = null;
                bool taskFinished = false;
                EasyARController.Instance.Worker.Run(() =>
                {
                    targetOptional = ImageTarget.createFromTargetData(buffer);
                    taskFinished = true;
                });

                while (!taskFinished)
                {
                    yield return 0;
                }
                if (targetOptional.OnSome)
                {
                    Target = targetOptional.Value;
                    if (Target != null && TargetAvailable != null)
                    {
                        TargetAvailable();
                    }
                }
                else
                {
                    throw new Exception("invalid buffer");
                }
            }
            UpdateTargetInTracker();
        }

        private void UpdateTargetInTracker()
        {
            if (Target == null)
            {
                return;
            }
            if (loader && loader != tracker)
            {
                loader.UnloadImageTarget(this, (target, status) =>
                {
                    if (TargetUnload != null)
                    {
                        TargetUnload(target, status);
                    }
                    if (status)
                    {
                        IsLoaded = false;
                    }
                });
                loader = null;
            }
            if (tracker && tracker != loader)
            {
                var trackerLoad = tracker;
                tracker.LoadImageTarget(this, (target, status) =>
                {
                    if (trackerLoad == tracker && !status)
                    {
                        loader = null;
                    }
                    UpdateScale();
                    if (TargetLoad != null)
                    {
                        TargetLoad(target, status);
                    }
                    IsLoaded = status;
                });
                loader = tracker;
            }
        }

        private void UpdateScale()
        {
            if (Target == null)
                return;
            scale = Target.scale();
            var vec3Unit = Vector3.one;
            if (HorizontalFlip)
            {
                vec3Unit.x = -vec3Unit.x;
            }
            transform.localScale = vec3Unit * scale;
            scaleX = transform.localScale.x;
            preHFlip = HorizontalFlip;
        }

        private void CheckScale()
        {
            if (Target == null)
                return;
            if (scaleX != transform.localScale.x)
            {
                Target.setScale(Math.Abs(transform.localScale.x));
                UpdateScale();
            }
            else if (scale != transform.localScale.y)
            {
                Target.setScale(Math.Abs(transform.localScale.y));
                UpdateScale();
            }
            else if (scale != transform.localScale.z)
            {
                Target.setScale(Math.Abs(transform.localScale.z));
                UpdateScale();
            }
            else if (scale != Target.scale())
            {
                UpdateScale();
            }
            else if (preHFlip != HorizontalFlip)
            {
                UpdateScale();
            }
        }

        /// <summary>
        /// <para xml:lang="en">Image data for target creation.</para>
        /// <para xml:lang="zh">创建target的图像数据。</para>
        /// </summary>
        [Serializable]
        public class ImageFileSourceData
        {
            /// <summary>
            /// <para xml:lang="en">File path type.</para>
            /// <para xml:lang="zh">文件路径类型。</para>
            /// </summary>
            public PathType PathType = PathType.StreamingAssets;
            /// <summary>
            /// <para xml:lang="en">File path.</para>
            /// <para xml:lang="zh">文件路径。</para>
            /// </summary>
            public string Path = string.Empty;
            /// <summary>
            /// <para xml:lang="en">Target name.</para>
            /// <para xml:lang="zh">Target名字。</para>
            /// </summary>
            public string Name = string.Empty;
            /// <summary>
            /// <para xml:lang="en">Target scale in meter. Reference <see cref="ImageTarget.scale"/>.</para>
            /// <para xml:lang="zh">Target图像的缩放比例，单位为米。参考<see cref="ImageTarget.scale"/>。</para>
            /// </summary>
            public float Scale = 0.1f;
        }

        /// <summary>
        /// <para xml:lang="en">Target data for target creation. Target scale and name are defined in the etd file.</para>
        /// <para xml:lang="zh">创建target的target data。Target名字和缩放在etd文件中定义。</para>
        /// </summary>
        [Serializable]
        public class TargetDataFileSourceData
        {
            /// <summary>
            /// <para xml:lang="en">File path type.</para>
            /// <para xml:lang="zh">文件路径类型。</para>
            /// </summary>
            public PathType PathType = PathType.StreamingAssets;
            /// <summary>
            /// <para xml:lang="en">File path.</para>
            /// <para xml:lang="zh">文件路径。</para>
            /// </summary>
            public string Path = string.Empty;
        }

#if UNITY_EDITOR
        /// <summary>
        /// <para xml:lang="en"><see cref="Gizmos"/> data. Used for <see cref="ImageTarget"/> gizmo display.</para>
        /// <para xml:lang="zh"><see cref="Gizmos"/>数据，用于在编辑器中显示<see cref="ImageTarget"/>的gizmo。</para>
        /// </summary>
        public class GizmoStorage
        {
            public string Signature;
            public Texture2D Texture;
            public Material Material;
            public float Scale = 0.1f;
            public float ScaleX = 0.1f;
            public bool HorizontalFlip;
        }
#endif
    }
}
