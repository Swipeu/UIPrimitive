using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Swipeu.UIPrimitive
{
    [ExecuteAlways]
    public abstract class BehaviourCopyBase<OriginalType, CopyType> : MonoBehaviour
        where OriginalType : Graphic
        where CopyType : Component
    {
        [SerializeField] RectTransform copyRoot;
        [SerializeField] RectTransform copyInstance;

        bool isDirty = false;
        Vector3 cachedPosition;

        CopyType _instanceComponent;
        protected CopyType InstanceComponent
        {
            get
            {

                if (_instanceComponent == null && copyInstance != null)
                    _instanceComponent = copyInstance.GetComponent<CopyType>();

                return _instanceComponent;
            }
        }

        OriginalType _originalComponent;
        protected OriginalType OriginalComponent
        {
            get
            {
                if (_originalComponent == null)
                    _originalComponent = GetComponent<OriginalType>();

                return _originalComponent;
            }
        }

        public RectTransform CopyRoot
        {
            get
            {
                return copyRoot;
            }
            set
            {
                copyRoot = value;
                TryInstantiate();
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (copyInstance == null || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var instance = copyInstance.gameObject;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (instance) GameObject.DestroyImmediate(instance);
            };
        }
#endif
        void OnCanvasGroupChanged()
        {
            SetDirty();
        }

        protected virtual void OnEnable()
        {
            OriginalComponent.RegisterDirtyLayoutCallback(SetDirty);
            OriginalComponent.RegisterDirtyMaterialCallback(SetDirty);
            OriginalComponent.RegisterDirtyVerticesCallback(SetDirty);
            SetDirty();
        }

        protected virtual void OnDisable()
        {
            OriginalComponent.UnregisterDirtyLayoutCallback(SetDirty);
            OriginalComponent.UnregisterDirtyMaterialCallback(SetDirty);
            OriginalComponent.UnregisterDirtyVerticesCallback(SetDirty);

            DisableInstance();
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;

                if (transform.localPosition != cachedPosition)
                {
                    cachedPosition = transform.position;
                    SetDirty();
                }
            }

            if (isDirty)
            {
                isDirty = false;

                Refresh();
            }
        }

        void LateUpdate()
        {
            if (isDirty)
            {
                isDirty = false;

                Refresh();
            }
        }

        public void DisableInstance()
        {
            if (copyInstance == null)
                return;

            copyInstance.gameObject.SetActive(false);
        }

        public void SetDirty()
        {
            isDirty = true;
        }

        void Refresh()
        {
            if (!enabled)
                return;

            if (copyInstance == null)
            {
                TryInstantiate();
                if (copyInstance == null)
                    return;
            }

            if (OriginalComponent == null)
                return;

            if (!copyInstance.gameObject.activeSelf)
                copyInstance.gameObject.SetActive(true);

            copyInstance.pivot = ((RectTransform)transform).pivot;
            copyInstance.position = transform.position;
            copyInstance.localPosition = (Vector2)copyInstance.localPosition + GetOffset(); 

            copyInstance.sizeDelta = new Vector2(OriginalComponent.rectTransform.rect.width, OriginalComponent.rectTransform.rect.height);
            copyInstance.localScale = GetScale();

            OnCopy();

            copyInstance.ForceUpdateRectTransforms();
        }
        void TryInstantiate()
        {
            if (CopyRoot == null)
                return;

            var instance = new GameObject($"{gameObject.name} Copy");
            instance.transform.SetParent(CopyRoot);
            instance.transform.localScale = Vector3.one;
            instance.transform.localRotation = Quaternion.identity;

            instance.gameObject.AddComponent<CopyType>();
            copyInstance = instance.GetComponent<RectTransform>();
            copyInstance.pivot = CopyRoot.pivot;

            var layoutElement = instance.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

#if UNITY_EDITOR

            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(instance);
#endif
            OnInstantiate();
        }

        abstract protected void OnCopy();
        abstract protected void OnInstantiate();
        virtual protected Vector2 GetOffset() => Vector2.zero;
        virtual protected Vector2 GetScale() => Vector2.one;
    }
}
