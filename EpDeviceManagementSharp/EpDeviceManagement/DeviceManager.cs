using System;
using EpDeviceManagement.Contracts;

namespace EpDeviceManagement
{
    public class DeviceManager
    {
        private readonly IStorage storage;

        public DeviceManager(IStorage storage)
        {
            this.storage = storage;
        }

        public void Tick()
        {
            var ratio = storage.CurrentStateOfCharge / storage.TotalCapacity;
        }
    }
}