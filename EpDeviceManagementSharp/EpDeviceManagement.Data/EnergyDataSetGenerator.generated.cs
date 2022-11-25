using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using EpDeviceManagement.Data.Generation;
using EpDeviceManagement.Data.Generation.Abstractions;

namespace EpDeviceManagement.Data;

public class EnergyDataSet
{
    public DateTimeOffset utc_timestamp { get; set; }

    public DateTimeOffset cet_cest_timestamp { get; set; }

    public double? DE_KN_industrial1_grid_import { get; set; }

    public double? DE_KN_industrial1_pv_1 { get; set; }

    public double? DE_KN_industrial1_pv_2 { get; set; }

    public double? DE_KN_industrial2_grid_import { get; set; }

    public double? DE_KN_industrial2_pv { get; set; }

    public double? DE_KN_industrial2_storage_charge { get; set; }

    public double? DE_KN_industrial2_storage_decharge { get; set; }

    public double? DE_KN_industrial3_area_offices { get; set; }

    public double? DE_KN_industrial3_area_room_1 { get; set; }

    public double? DE_KN_industrial3_area_room_2 { get; set; }

    public double? DE_KN_industrial3_area_room_3 { get; set; }

    public double? DE_KN_industrial3_area_room_4 { get; set; }

    public double? DE_KN_industrial3_compressor { get; set; }

    public double? DE_KN_industrial3_cooling_aggregate { get; set; }

    public double? DE_KN_industrial3_cooling_pumps { get; set; }

    public double? DE_KN_industrial3_dishwasher { get; set; }

    public double? DE_KN_industrial3_ev { get; set; }

    public double? DE_KN_industrial3_grid_import { get; set; }

    public double? DE_KN_industrial3_machine_1 { get; set; }

    public double? DE_KN_industrial3_machine_2 { get; set; }

    public double? DE_KN_industrial3_machine_3 { get; set; }

    public double? DE_KN_industrial3_machine_4 { get; set; }

    public double? DE_KN_industrial3_machine_5 { get; set; }

    public double? DE_KN_industrial3_pv_facade { get; set; }

    public double? DE_KN_industrial3_pv_roof { get; set; }

    public double? DE_KN_industrial3_refrigerator { get; set; }

    public double? DE_KN_industrial3_ventilation { get; set; }

    public double? DE_KN_public1_grid_import { get; set; }

    public double? DE_KN_public2_grid_import { get; set; }

    public double? DE_KN_residential1_dishwasher { get; set; }

    public double? DE_KN_residential1_freezer { get; set; }

    public double? DE_KN_residential1_grid_import { get; set; }

    public double? DE_KN_residential1_heat_pump { get; set; }

    public double? DE_KN_residential1_pv { get; set; }

    public double? DE_KN_residential1_washing_machine { get; set; }

    public double? DE_KN_residential2_circulation_pump { get; set; }

    public double? DE_KN_residential2_dishwasher { get; set; }

    public double? DE_KN_residential2_freezer { get; set; }

    public double? DE_KN_residential2_grid_import { get; set; }

    public double? DE_KN_residential2_washing_machine { get; set; }

    public double? DE_KN_residential3_circulation_pump { get; set; }

    public double? DE_KN_residential3_dishwasher { get; set; }

    public double? DE_KN_residential3_freezer { get; set; }

    public double? DE_KN_residential3_grid_export { get; set; }

    public double? DE_KN_residential3_grid_import { get; set; }

    public double? DE_KN_residential3_pv { get; set; }

    public double? DE_KN_residential3_refrigerator { get; set; }

    public double? DE_KN_residential3_washing_machine { get; set; }

    public double? DE_KN_residential4_dishwasher { get; set; }

    public double? DE_KN_residential4_ev { get; set; }

    public double? DE_KN_residential4_freezer { get; set; }

    public double? DE_KN_residential4_grid_export { get; set; }

    public double? DE_KN_residential4_grid_import { get; set; }

    public double? DE_KN_residential4_heat_pump { get; set; }

    public double? DE_KN_residential4_pv { get; set; }

    public double? DE_KN_residential4_refrigerator { get; set; }

    public double? DE_KN_residential4_washing_machine { get; set; }

    public double? DE_KN_residential5_dishwasher { get; set; }

    public double? DE_KN_residential5_grid_import { get; set; }

    public double? DE_KN_residential5_refrigerator { get; set; }

    public double? DE_KN_residential5_washing_machine { get; set; }

    public double? DE_KN_residential6_circulation_pump { get; set; }

    public double? DE_KN_residential6_dishwasher { get; set; }

    public double? DE_KN_residential6_freezer { get; set; }

    public double? DE_KN_residential6_grid_export { get; set; }

    public double? DE_KN_residential6_grid_import { get; set; }

    public double? DE_KN_residential6_pv { get; set; }

    public double? DE_KN_residential6_washing_machine { get; set; }

    public string interpolated { get;set; }

}

public class PowerDataSet
{
    public DateTimeOffset utc_timestamp { get; set; }

    public DateTimeOffset cet_cest_timestamp { get; set; }

    [Ignore]
    public double DE_KN_industrial1_grid_import { get; set; }

    [Ignore]
    public double DE_KN_industrial1_pv_1 { get; set; }

    [Ignore]
    public double DE_KN_industrial1_pv_2 { get; set; }

    [Ignore]
    public double DE_KN_industrial2_grid_import { get; set; }

    [Ignore]
    public double DE_KN_industrial2_pv { get; set; }

    [Ignore]
    public double DE_KN_industrial2_storage_charge { get; set; }

    [Ignore]
    public double DE_KN_industrial2_storage_decharge { get; set; }

    [Ignore]
    public double DE_KN_industrial3_area_offices { get; set; }

    [Ignore]
    public double DE_KN_industrial3_area_room_1 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_area_room_2 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_area_room_3 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_area_room_4 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_compressor { get; set; }

    [Ignore]
    public double DE_KN_industrial3_cooling_aggregate { get; set; }

    [Ignore]
    public double DE_KN_industrial3_cooling_pumps { get; set; }

    [Ignore]
    public double DE_KN_industrial3_dishwasher { get; set; }

    [Ignore]
    public double DE_KN_industrial3_ev { get; set; }

    [Ignore]
    public double DE_KN_industrial3_grid_import { get; set; }

    [Ignore]
    public double DE_KN_industrial3_machine_1 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_machine_2 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_machine_3 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_machine_4 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_machine_5 { get; set; }

    [Ignore]
    public double DE_KN_industrial3_pv_facade { get; set; }

    [Ignore]
    public double DE_KN_industrial3_pv_roof { get; set; }

    [Ignore]
    public double DE_KN_industrial3_refrigerator { get; set; }

    [Ignore]
    public double DE_KN_industrial3_ventilation { get; set; }

    [Ignore]
    public double DE_KN_public1_grid_import { get; set; }

    [Ignore]
    public double DE_KN_public2_grid_import { get; set; }

    public double DE_KN_residential1_dishwasher { get; set; }

    public double DE_KN_residential1_freezer { get; set; }

    public double DE_KN_residential1_grid_import { get; set; }

    public double DE_KN_residential1_heat_pump { get; set; }

    public double DE_KN_residential1_pv { get; set; }

    public double DE_KN_residential1_washing_machine { get; set; }

    public double DE_KN_residential2_circulation_pump { get; set; }

    public double DE_KN_residential2_dishwasher { get; set; }

    public double DE_KN_residential2_freezer { get; set; }

    public double DE_KN_residential2_grid_import { get; set; }

    public double DE_KN_residential2_washing_machine { get; set; }

    public double DE_KN_residential3_circulation_pump { get; set; }

    public double DE_KN_residential3_dishwasher { get; set; }

    public double DE_KN_residential3_freezer { get; set; }

    public double DE_KN_residential3_grid_export { get; set; }

    public double DE_KN_residential3_grid_import { get; set; }

    public double DE_KN_residential3_pv { get; set; }

    public double DE_KN_residential3_refrigerator { get; set; }

    public double DE_KN_residential3_washing_machine { get; set; }

    public double DE_KN_residential4_dishwasher { get; set; }

    public double DE_KN_residential4_ev { get; set; }

    public double DE_KN_residential4_freezer { get; set; }

    public double DE_KN_residential4_grid_export { get; set; }

    public double DE_KN_residential4_grid_import { get; set; }

    public double DE_KN_residential4_heat_pump { get; set; }

    public double DE_KN_residential4_pv { get; set; }

    public double DE_KN_residential4_refrigerator { get; set; }

    public double DE_KN_residential4_washing_machine { get; set; }

    public double DE_KN_residential5_dishwasher { get; set; }

    public double DE_KN_residential5_grid_import { get; set; }

    public double DE_KN_residential5_refrigerator { get; set; }

    public double DE_KN_residential5_washing_machine { get; set; }

    public double DE_KN_residential6_circulation_pump { get; set; }

    public double DE_KN_residential6_dishwasher { get; set; }

    public double DE_KN_residential6_freezer { get; set; }

    public double DE_KN_residential6_grid_export { get; set; }

    public double DE_KN_residential6_grid_import { get; set; }

    public double DE_KN_residential6_pv { get; set; }

    public double DE_KN_residential6_washing_machine { get; set; }

    public string interpolated { get;set; }

}