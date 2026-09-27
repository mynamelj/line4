using System;
using System.Collections.Generic;
using System.Linq;

namespace MES.Comm
{
    /// <summary>
    /// 固定长度的先进先出队列，线程安全。
    /// 每次入队若超过最大长度，自动剔除最旧的元素。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class FixedQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly int _capacity;
        private readonly object _lock = new object();

        public FixedQueue(int capacity = 20)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));
            }
            _capacity = capacity;
        }

        /// <summary>
        /// 队列最大容量
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// 当前队列中元素个数
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        /// <summary>
        /// 入队。如果入队后数量超过容量限制，则将最旧的元素移出队列。
        /// </summary>
        /// <param name="item">入队项</param>
        public void Enqueue(T item)
        {
            lock (_lock)
            {
                _queue.Enqueue(item);
                while (_queue.Count > _capacity)
                {
                    _queue.Dequeue();
                }
            }
        }

        /// <summary>
        /// 入队或更新。若匹配到已有元素则执行 updateAction 进行覆盖更新，不追加队列；
        /// 若不存在匹配元素则入队，并在超出容量时剔除最旧元素。
        /// </summary>
        /// <param name="matchPredicate">匹配条件</param>
        /// <param name="updateAction">针对已有元素的更新操作</param>
        /// <param name="newItem">未匹配时新增入队的项</param>
        /// <returns>如果是新增入队返回 true；如果是覆盖已有项返回 false</returns>
        public bool EnqueueOrUpdate(Func<T, bool> matchPredicate, Action<T> updateAction, T newItem)
        {
            lock (_lock)
            {
                var existing = _queue.FirstOrDefault(matchPredicate);
                if (existing != null)
                {
                    updateAction(existing);
                    return false;
                }

                _queue.Enqueue(newItem);
                while (_queue.Count > _capacity)
                {
                    _queue.Dequeue();
                }
                return true;
            }
        }

        /// <summary>
        /// 获取当前队列的快照列表（按从旧到新排列）
        /// </summary>
        public List<T> ToList()
        {
            lock (_lock)
            {
                return _queue.ToList();
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
            }
        }
    }
}
