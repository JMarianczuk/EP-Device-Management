
DROP TABLE IF EXISTS successful_counts_sc;
DROP TABLE IF EXISTS successful_counts_scd;
DROP TABLE IF EXISTS successful_counts_scdt;

DROP TABLE IF EXISTS simulation_with_counts_sc;
DROP TABLE IF EXISTS simulation_with_counts_scd;
DROP TABLE IF EXISTS simulation_with_counts_scdt;

DROP TABLE IF EXISTS distinct_data;
DROP TABLE IF EXISTS distinct_timeStep;
DROP TABLE IF EXISTS distinct_strategy;
DROP TABLE IF EXISTS distinct_packetSize;
DROP TABLE IF EXISTS distinct_probability;
DROP TABLE IF EXISTS distinct_battery;
DROP TABLE IF EXISTS distinct_strategy_configuration;

DROP TABLE IF EXISTS energy_count_packetSize_strategy;
DROP TABLE IF EXISTS energy_count_packetSize_data;
DROP TABLE IF EXISTS energy_count_packetSize_battery;
DROP TABLE IF EXISTS energy_count_packetSize_probability;

DROP TABLE IF EXISTS energy_count_probability_strategy;
DROP TABLE IF EXISTS energy_count_probability_data;
DROP TABLE IF EXISTS energy_count_probability_battery;
DROP TABLE IF EXISTS energy_count_probability_packetSize;

DROP TABLE IF EXISTS total_count_packetSize_strategy;
DROP TABLE IF EXISTS total_count_packetSize_data;
DROP TABLE IF EXISTS total_count_packetSize_battery;
DROP TABLE IF EXISTS total_count_packetSize_packetSize;

DROP TABLE IF EXISTS total_count_probability_strategy;
DROP TABLE IF EXISTS total_count_probability_data;
DROP TABLE IF EXISTS total_count_probability_battery;
DROP TABLE IF EXISTS total_count_probability_probability;