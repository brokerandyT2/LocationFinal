using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Location.Core.BDD.Tests.Drivers
{
    public class TipTypeDriver
    {
        private readonly ITipTypeRepository _tipTypeRepository;

        public TipTypeDriver(ITipTypeRepository tipTypeRepository)
        {
            _tipTypeRepository = tipTypeRepository ?? throw new ArgumentNullException(nameof(tipTypeRepository));
        }

        public async Task<TipType> CreateTipTypeAsync(string name, string localization = "en-US")
        {
            var tipType = new TipType(name);
            tipType.SetLocalization(localization);

            var createdTipType = await _tipTypeRepository.AddAsync(tipType);
            return createdTipType;
        }

        public async Task<TipType?> GetTipTypeByIdAsync(int id)
        {
            return await _tipTypeRepository.GetByIdAsync(id);
        }

        public async Task<TipType?> GetTipTypeByNameAsync(string name)
        {
            return await _tipTypeRepository.GetByNameAsync(name);
        }

        public async Task<TipType?> GetTipTypeWithTipsAsync(int id)
        {
            return await _tipTypeRepository.GetWithTipsAsync(id);
        }

        public async Task<System.Collections.Generic.IEnumerable<TipType>> GetAllTipTypesAsync()
        {
            return await _tipTypeRepository.GetAllAsync();
        }

        public async Task UpdateTipTypeAsync(TipType tipType)
        {
            if (tipType == null)
                throw new ArgumentNullException(nameof(tipType));

            await _tipTypeRepository.UpdateAsync(tipType); // FIXED: Use async method
        }

        public async Task UpdateTipTypeNameAsync(string currentName, string newName)
        {
            var tipType = await _tipTypeRepository.GetByNameAsync(currentName);
            if (tipType != null)
            {
                // Update the name using reflection since it's likely a private setter
                SetPrivateProperty(tipType, "_name", newName);
                await _tipTypeRepository.UpdateAsync(tipType); // FIXED: Use async method
            }
        }

        public async Task DeleteTipTypeAsync(TipType tipType)
        {
            if (tipType == null)
                throw new ArgumentNullException(nameof(tipType));

            await _tipTypeRepository.DeleteAsync(tipType); // FIXED: Use async method
        }

        public async Task DeleteTipTypeByNameAsync(string name)
        {
            var tipType = await _tipTypeRepository.GetByNameAsync(name);
            if (tipType != null)
            {
                await _tipTypeRepository.DeleteAsync(tipType); // FIXED: Use async method
            }
        }

        public async Task DeleteTipTypeByIdAsync(int id)
        {
            var tipType = await _tipTypeRepository.GetByIdAsync(id);
            if (tipType != null)
            {
                await _tipTypeRepository.DeleteAsync(tipType); // FIXED: Use async method
            }
        }

        public async Task<bool> TipTypeExistsAsync(string name)
        {
            var tipType = await _tipTypeRepository.GetByNameAsync(name);
            return tipType != null;
        }

        public async Task<bool> TipTypeExistsAsync(int id)
        {
            var tipType = await _tipTypeRepository.GetByIdAsync(id);
            return tipType != null;
        }

        public async Task<int> GetTipTypeCountAsync()
        {
            var tipTypes = await _tipTypeRepository.GetAllAsync();
            return System.Linq.Enumerable.Count(tipTypes);
        }

        public TipType CreateTipTypeInstance(string name, string localization = "en-US")
        {
            var tipType = new TipType(name);
            tipType.SetLocalization(localization);
            return tipType;
        }

        public void AddTipToTipType(TipType tipType, Tip tip)
        {
            if (tipType == null)
                throw new ArgumentNullException(nameof(tipType));
            if (tip == null)
                throw new ArgumentNullException(nameof(tip));

            tipType.AddTip(tip);
        }

        public void RemoveTipFromTipType(TipType tipType, Tip tip)
        {
            if (tipType == null)
                throw new ArgumentNullException(nameof(tipType));
            if (tip == null)
                throw new ArgumentNullException(nameof(tip));

            tipType.RemoveTip(tip);
        }

        // Helper method for setting private properties via reflection
        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
        }

        // Helper method for getting private properties via reflection
        private T? GetPrivateProperty<T>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (property != null && property.CanRead)
            {
                return (T?)property.GetValue(obj);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field != null ? (T?)field.GetValue(obj) : default(T);
            }
        }
    }
}