
//using EpDeviceManagement.Contracts;
//using UnitsNet;

//namespace EpDeviceManagement.Control.Strategy;

//public class ModelPredictiveController : IEpDeviceController
//{
//    private IStorage battery;

//    public ModelPredictiveController(IStorage battery)
//    {
//        this.battery = battery;
//    }

//    public ControlDecision DoControl(
//        int dataPoint,
//        TimeSpan timeStep,
//        ILoad load,
//        IGenerator generator,
//        TransferResult lastTransferResult)
//    {
//        (
//            var deferrable,
//            var interruptible,
//            var iloads
//        ) = load.GroupByDerivingType<IDeferrableLoad, IInterruptibleLoad, ILoad>();

//        // come to a decision if energy needs to be added to battery
//        // example: per step 1kWh needs to be added for 5 steps
//        //          per step 0.5kWh needs to be shifted to 3 steps into the future
//        var totalShiftPower = Energy.FromKilowattHours(0.5) / timeStep;

//        foreach (var def in deferrable)
//        {
//            if (totalShiftPower <= Power.Zero)
//            {
//                break;
//            }
//            if (
//                !def.IsDeferred
//                && def.MaximumPossibleDeferral > 3 * timeStep)
//            {
//                def.DeferFor(3 * timeStep);
//                totalShiftPower -= def.MomentaryDemand;
//            }
//        }

//        /*
//         * Priorities of the available decisions:
//         * 1. Request to send/receive packets to/from the grid
//         * -> send 3 packets because the predicted SoC of the battery would otherwise exceed the capacity at the end of the day
//         * 
//         * 2. divert energy to / restrict energy from TCLs
//         * -> heat the building / let the building lose heat
//         * 
//         * => this order is not perfect. e.g. prefer giving energy to the TCL over sending it into the grid, but prefer receiving a packet over restricting energy from a TCL
//         * 
//         * 3. defer energy use of a connected deferrable load
//         * -> washing machine indicates it is ready to start, delay the start to 15 minutes from now
//         * 
//         * 4. interrupt a connected interruptible load
//         * -> stop a dishwasher for 15 minutes, after which it resumes operation
//         * 
//         * 5. disable a connected generator
//         * -> switch off 1/3 of the solar panels because it would exceed the battery's capacity
//         * 
//         * 6. decide if until the next control step, incoming requests for packet transfers should be accepted
//         * -> for sending a packet, receiving one, or even both
//         * -> this has the lowest priority, because it is considered out of scope for the controller, as in it can not be guaranteed that a request will be made
//         */

//        throw new NotImplementedException();
//    }

//    //public static void IpOptSample()
//    //{
//    //    var variables = new double[2];
//    //    var lowerBounds = new double[variables.Length];
//    //    var upperBounds = new double[variables.Length];
//    //    var constraintDefinitions = new double[2];
//    //    var constraintLowerBounds = new double[constraintDefinitions.Length];
//    //    var constraintUpperBounds = new double[constraintDefinitions.Length];
//    //    int nonZeroInConstraintJacobian = 0;
//    //    int nonZeroInHessianOfLagrangian = 0;
//    //    Eval_F_CB objectiveFunction = ObjectiveFunction;
//    //    Eval_G_CB constraintFunction = ConstraintFunction;
//    //    Eval_Grad_F_CB objectiveFunctionGradient = ObjectiveFunctionGradient;
//    //    Eval_Jac_G_CB constraintFunctionJacobian = ConstraintFunctionJacobian;
//    //    Eval_H_CB hessianOfLagrangian = HessianOfLagrangian;
//    //    var ipopt = IpoptAdapter.CreateIpoptProblem(
//    //        variables.Length,
//    //        lowerBounds,
//    //        upperBounds,
//    //        constraintDefinitions.Length,
//    //        constraintLowerBounds,
//    //        constraintUpperBounds,
//    //        nonZeroInConstraintJacobian,
//    //        nonZeroInHessianOfLagrangian,
//    //        IpoptIndexStyle.C,
//    //        objectiveFunction,
//    //        constraintFunction,
//    //        objectiveFunctionGradient,
//    //        constraintFunctionJacobian,
//    //        hessianOfLagrangian
//    //    );

//    //    var input = new double[variables.Length];
//    //    var constraints = new double[constraintDefinitions.Length];
//    //    double[] constraintMultipliers = null;
//    //    double[] lowerBoundsMultipliers = null;
//    //    double[] upperBoundMultipliers = null;
//    //    var ret = IpoptAdapter.IpoptSolve(
//    //        ipopt,
//    //        input, 
//    //        constraints,
//    //        out var objectiveValue,
//    //        constraintMultipliers,
//    //        lowerBoundsMultipliers,
//    //        upperBoundMultipliers,
//    //        IntPtr.Zero);

//    //    IpoptAdapter.FreeIpoptProblem(ipopt);
//    //}

//    //private static IpoptBoolType ObjectiveFunction(
//    //    int n,
//    //    double[] x,
//    //    IpoptBoolType new_x,
//    //    out double obj_value,
//    //    IntPtr p_user_data)
//    //{
//    //    throw new NotImplementedException();
//    //}

//    //private static IpoptBoolType ConstraintFunction(
//    //    int n,
//    //    double[] x,
//    //    IpoptBoolType new_x,
//    //    int m,
//    //    double[] g,
//    //    IntPtr p_user_data)
//    //{
//    //    throw new NotImplementedException();
//    //}

//    //private static IpoptBoolType ObjectiveFunctionGradient(
//    //    int n,
//    //    double[] x,
//    //    IpoptBoolType new_x,
//    //    double[] grad_f,
//    //    IntPtr p_user_data)
//    //{
//    //    throw new NotImplementedException();
//    //}

//    //private static IpoptBoolType ConstraintFunctionJacobian(
//    //    int n,
//    //    double[] x,
//    //    IpoptBoolType new_x,
//    //    int m,
//    //    int nele_jac,
//    //    int[] irow,
//    //    int[] jcol,
//    //    double[] values,
//    //    IntPtr p_user_data)
//    //{
//    //    throw new NotImplementedException();
//    //}

//    //private static IpoptBoolType HessianOfLagrangian(
//    //    int n,
//    //    double[] x,
//    //    IpoptBoolType new_x,
//    //    double obj_factor,
//    //    int m,
//    //    double[] lambda,
//    //    IpoptBoolType new_lambda,
//    //    int nele_hess,
//    //    int[] irow,
//    //    int[] jcol,
//    //    double[] values,
//    //    IntPtr p_user_data)
//    //{
//    //    throw new NotImplementedException();
//    //}

//    public string Name => nameof(ModelPredictiveController);

//    public string Configuration => string.Empty;

//    public string PrettyConfiguration => string.Empty;

//    public bool RequestsOutgoingPackets => false;
//}