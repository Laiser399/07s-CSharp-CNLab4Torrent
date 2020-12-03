using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab4_Client
{
    class Speedometer
    {
        private long _msRange;
        private Queue<Item> _items = new Queue<Item>();
        private long _sum = 0;
        
        public Speedometer(long msRange)
        {
            _msRange = msRange;
        }

        public void Add(long bytesCount)
        {
            RemoveLost();
            _items.Enqueue(new Item
            {
                Time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                BytesCount = bytesCount
            });
            _sum += bytesCount;
        }

        public double GetSpeedPerSecond()
        {
            RemoveLost();
            return (double)_sum / _msRange * 1000;
        }

        public string GetSpeedStrRepr()
        {
            double speed = GetSpeedPerSecond();
            int divCount = 0;
            while (speed > 1024 && divCount < 3)
            {
                speed /= 1024;
                divCount++;
            }
           
            switch (divCount)
            {
                case 0:
                    return $"{speed.ToString("F1")} B/s";
                case 1:
                    return $"{speed.ToString("F1")} KB/s";
                case 2:
                    return $"{speed.ToString("F1")} MB/s";
                default:// 3
                    return $"{speed.ToString("F1")} GB/s";
            }
        }

        private void RemoveLost()
        {
            long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            while (_items.Count > 0 && currentTime - _items.Peek().Time > _msRange)
                _sum -= _items.Dequeue().BytesCount;
        }


        private class Item
        {
            public long Time;
            public long BytesCount;
        }
    }
}
