using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control;

public class ModelPredictiveController : IEpDeviceController
{
    private IStorage battery;

    public ModelPredictiveController(IStorage battery)
    {
        this.battery = battery;
    }

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads)
    {
        (
            var deferrable,
            var interruptible,
            loads
        ) = loads.GroupByDerivingType<IDeferrableLoad, IInterruptibleLoad, ILoad>();
        
        // come to a decision if energy needs to be added to battery
        // example: per step 1kWh needs to be added for 5 steps
        //          per step 0.5kWh needs to be shifted to 3 steps into the future
        var totalShiftPower = Energy.FromKilowattHours(0.5) / timeStep;

        foreach (var def in deferrable)
        {
            if (totalShiftPower <= Power.Zero)
            {
                break;
            }
            if (
                !def.IsDeferred
                && def.MaximumPossibleDeferral > 3 * timeStep)
            {
                def.DeferFor(3 * timeStep);
                totalShiftPower -= def.CurrentDemand;
            }
        }
        
        /*
         * Priorities of the available decisions:
         * 1. Request to send/receive packets to/from the grid
         * -> send 3 packets because the predicted SoC of the battery would otherwise exceed the capacity at the end of the day
         * 
         * 2. divert energy to / restrict energy from TCLs
         * -> heat the building / let the building lose heat
         * 
         * => this order is not perfect. e.g. prefer giving energy to the TCL over sending it into the grid, but prefer receiving a packet over restricting energy from a TCL
         * 
         * 3. defer energy use of a connected deferrable load
         * -> washing machine indicates it is ready to start, delay the start to 15 minutes from now
         * 
         * 4. interrupt a connected interruptible load
         * -> stop a dishwasher for 15 minutes, after which it resumes operation
         * 
         * 5. disable a connected generator
         * -> switch off 1/3 of the solar panels because it would exceed the battery's capacity
         * 
         * 6. decide if until the next control step, incoming requests for packet transfers should be accepted
         * -> for sending a packet, receiving one, or even both
         * -> this has the lowest priority, because it is considered out of scope for the controller, as in it can not be guaranteed that a request will be made
         */

        throw new NotImplementedException();
    }
}