namespace MpcForBuildings;

public class ExampleMpc
{
    public void Example()
    {
        var formulation = new MpcFormulation();
        var timeStep = TimeSpan.FromMinutes(15);
        var predictionHorizon = TimeSpan.FromHours(8);
        var controlHorizon = 7 * timeStep;
        //formulation.Solve()
    }
}