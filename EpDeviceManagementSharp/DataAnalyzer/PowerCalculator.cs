using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using CsvHelper.TypeConversion;
using EpDeviceManagement.Data;
using UnitsNet;

namespace DataAnalyzer;

public class PowerCalculator
{
    public static async Task CalculateAsync()
    {
        var files = new[]
        {
            //1,
            //5,
            //15,
            //60,
            //240,
            //360,
            1440,
        };
        foreach (var f in files)
        {
            Console.WriteLine($"Writing {f}min");
            using var reader = new StreamReader($"household_data_{f}min_singleindex.csv");
            var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture);
            using var csvReader = new CsvReader(reader, csvConfiguration);
            await using var writer = new StreamWriter($"household_data_{f}min_power.csv");
            await using var csvWriter = new CsvWriter(writer, csvConfiguration);
            csvWriter.Context.TypeConverterCache.AddConverter<DateTimeOffset>(new DateTimeOffsetTypeConverter());

            var timeStep = TimeSpan.FromMinutes(f);

            csvWriter.WriteHeader<PowerDataSet>();
            await csvWriter.NextRecordAsync();
            var input = csvReader.GetRecordsAsync<EnergyDataSet>();
            var last = new EnergyDataSet
            {
                utc_timestamp = default,
                cet_cest_timestamp = default,
                DE_KN_industrial1_grid_import = 0,
                DE_KN_industrial1_pv_1 = 0,
                DE_KN_industrial1_pv_2 = 0,
                DE_KN_industrial2_grid_import = 0,
                DE_KN_industrial2_pv = 0,
                DE_KN_industrial2_storage_charge = 0,
                DE_KN_industrial2_storage_decharge = 0,
                DE_KN_industrial3_area_offices = 0,
                DE_KN_industrial3_area_room_1 = 0,
                DE_KN_industrial3_area_room_2 = 0,
                DE_KN_industrial3_area_room_3 = 0,
                DE_KN_industrial3_area_room_4 = 0,
                DE_KN_industrial3_compressor = 0,
                DE_KN_industrial3_cooling_aggregate = 0,
                DE_KN_industrial3_cooling_pumps = 0,
                DE_KN_industrial3_dishwasher = 0,
                DE_KN_industrial3_ev = 0,
                DE_KN_industrial3_grid_import = 0,
                DE_KN_industrial3_machine_1 = 0,
                DE_KN_industrial3_machine_2 = 0,
                DE_KN_industrial3_machine_3 = 0,
                DE_KN_industrial3_machine_4 = 0,
                DE_KN_industrial3_machine_5 = 0,
                DE_KN_industrial3_pv_facade = 0,
                DE_KN_industrial3_pv_roof = 0,
                DE_KN_industrial3_refrigerator = 0,
                DE_KN_industrial3_ventilation = 0,
                DE_KN_public1_grid_import = 0,
                DE_KN_public2_grid_import = 0,
                DE_KN_residential1_dishwasher = 0,
                DE_KN_residential1_freezer = 0,
                DE_KN_residential1_grid_import = 0,
                DE_KN_residential1_heat_pump = 0,
                DE_KN_residential1_pv = 0,
                DE_KN_residential1_washing_machine = 0,
                DE_KN_residential2_circulation_pump = 0,
                DE_KN_residential2_dishwasher = 0,
                DE_KN_residential2_freezer = 0,
                DE_KN_residential2_grid_import = 0,
                DE_KN_residential2_washing_machine = 0,
                DE_KN_residential3_circulation_pump = 0,
                DE_KN_residential3_dishwasher = 0,
                DE_KN_residential3_freezer = 0,
                DE_KN_residential3_grid_export = 0,
                DE_KN_residential3_grid_import = 0,
                DE_KN_residential3_pv = 0,
                DE_KN_residential3_refrigerator = 0,
                DE_KN_residential3_washing_machine = 0,
                DE_KN_residential4_dishwasher = 0,
                DE_KN_residential4_ev = 0,
                DE_KN_residential4_freezer = 0,
                DE_KN_residential4_grid_export = 0,
                DE_KN_residential4_grid_import = 0,
                DE_KN_residential4_heat_pump = 0,
                DE_KN_residential4_pv = 0,
                DE_KN_residential4_refrigerator = 0,
                DE_KN_residential4_washing_machine = 0,
                DE_KN_residential5_dishwasher = 0,
                DE_KN_residential5_grid_import = 0,
                DE_KN_residential5_refrigerator = 0,
                DE_KN_residential5_washing_machine = 0,
                DE_KN_residential6_circulation_pump = 0,
                DE_KN_residential6_dishwasher = 0,
                DE_KN_residential6_freezer = 0,
                DE_KN_residential6_grid_export = 0,
                DE_KN_residential6_grid_import = 0,
                DE_KN_residential6_pv = 0,
                DE_KN_residential6_washing_machine = 0,
                interpolated = string.Empty,
            };
            var energyType = typeof(EnergyDataSet);
            var powerType = typeof(PowerDataSet);
            var energyProperties = energyType.GetProperties();
            var powerProperties = powerType.GetProperties();
            var properties = energyProperties
                .Where(p => p.PropertyType == typeof(double?))
                .Select(ep => (ep, pp: powerProperties.First(pp => pp.Name == ep.Name)));
            await foreach (var energy in input)
            {
                var power = new PowerDataSet();
                if (last.utc_timestamp == default)
                {
                    power.cet_cest_timestamp = energy.cet_cest_timestamp - timeStep;
                    power.utc_timestamp = energy.utc_timestamp - timeStep;
                    power.interpolated = string.Empty;
                }
                else
                {
                    power.cet_cest_timestamp = last.cet_cest_timestamp;
                    power.utc_timestamp = last.utc_timestamp;
                    power.interpolated = last.interpolated;

                    last.cet_cest_timestamp = energy.cet_cest_timestamp;
                    last.utc_timestamp = energy.utc_timestamp;
                    last.interpolated = energy.interpolated;
                }
                
                foreach (var (ep, pp) in properties)
                {
                    var last_entry = (double)ep.GetValue(last);
                    var now_entry = (double?)ep.GetValue(energy);
                    var last_energy = Energy.FromKilowattHours(last_entry);
                    var now_energy = Energy.FromKilowattHours(now_entry ?? last_entry);
                    var result = (now_energy - last_energy) / timeStep;
                    ep.SetValue(last, now_energy.KilowattHours);
                    pp.SetValue(power, result.Kilowatts);
                }
                csvWriter.WriteRecord(power);
                await csvWriter.NextRecordAsync();
            }
        }
    }
}