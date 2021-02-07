using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrooprWASM.Data.Settings;
using GrooprWASM.Model;

namespace GrooprWASM.Data.GroupGeneration
{
    public interface IGroupGenerationStrategy
    {
        public Task StartAsync(Action<List<GroupSet>, int, int> myAction, SettingsContainer sc);

    }
}