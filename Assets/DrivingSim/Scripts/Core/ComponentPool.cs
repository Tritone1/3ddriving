using System.Collections.Generic;
using UnityEngine;

namespace DrivingSim.Core
{
    public sealed class ComponentPool : MonoBehaviour
    {
        [SerializeField] private Component prefab;
        [SerializeField, Min(0)] private int prewarmCount = 8;
        private readonly Queue<Component> available = new Queue<Component>();

        private void Awake()
        {
            for (int i = 0; i < prewarmCount; i++) Release(Create());
        }

        public Component Get(Vector3 position, Quaternion rotation)
        {
            Component item = available.Count > 0 ? available.Dequeue() : Create();
            item.transform.SetPositionAndRotation(position, rotation);
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(Component item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(transform, false);
            available.Enqueue(item);
        }

        private Component Create()
        {
            Component item = Instantiate(prefab, transform);
            item.gameObject.SetActive(false);
            return item;
        }
    }
}
