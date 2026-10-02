using System;
using UnityEngine;

namespace DrivingSim.Core
{
    public interface ICurrencyWallet
    {
        int Balance { get; }
        event Action<int> BalanceChanged;
        bool TrySpend(int amount);
        void Add(int amount);
    }

    public abstract class WalletBehaviour : MonoBehaviour, ICurrencyWallet
    {
        public abstract int Balance { get; }
        public abstract event Action<int> BalanceChanged;
        public abstract bool TrySpend(int amount);
        public abstract void Add(int amount);
    }
}
