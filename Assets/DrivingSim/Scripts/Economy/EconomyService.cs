using System;
using DrivingSim.Core;
using DrivingSim.Save;
using UnityEngine;

namespace DrivingSim.Economy
{
    public sealed class EconomyService : WalletBehaviour
    {
        [SerializeField] private SaveService saveService;
        public override event Action<int> BalanceChanged;

        public override int Balance => saveService != null && saveService.Profile != null ? saveService.Profile.money : 0;

        public void Configure(SaveService saves)
        {
            saveService = saves;
            BalanceChanged?.Invoke(Balance);
        }

        public override bool TrySpend(int amount)
        {
            if (amount < 0 || saveService == null || saveService.Profile == null || Balance < amount) return false;
            saveService.Profile.money -= amount;
            BalanceChanged?.Invoke(Balance);
            saveService.Save();
            return true;
        }

        public override void Add(int amount)
        {
            if (amount <= 0 || saveService == null || saveService.Profile == null) return;
            saveService.Profile.money = Mathf.Max(0, saveService.Profile.money + amount);
            BalanceChanged?.Invoke(Balance);
            saveService.Save();
        }
    }
}
