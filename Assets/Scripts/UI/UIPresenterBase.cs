using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public abstract class UIPresenterBase : MonoBehaviour
    {
        protected UIDocument doc;
        protected VisualElement root;

        /// <summary>
        /// Call when the UIDocument is guaranteed to be ready.
        /// </summary>
        protected void AcquireRoot()
        {
            if (root != null) return;
            if (doc == null) doc = GetComponent<UIDocument>();
            root = doc.rootVisualElement;
        }

        /// <summary>
        /// Use when the UIDocument may not be ready immediately.
        /// </summary>
        protected IEnumerator WaitForRoot()
        {
            while (root == null)
            {
                if (doc == null) doc = GetComponent<UIDocument>();
                else root = doc.rootVisualElement;
                yield return null;
            }
        }
    }
}
