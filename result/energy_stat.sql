PRAGMA case_sensitive_like=ON;

DROP TABLE IF EXISTS energy_sc_stat;

CREATE TABLE
    energy_sc_stat
AS
SELECT
    strategy,
    configuration,
    success,
    min(energy_kwh_in) as minimum_in,
    max(energy_kwh_in) as maximum_in,
    avg(energy_kwh_in) as average_in,
    min(energy_kwh_out) as minimum_out,
    max(energy_kwh_out) as maximum_out,
    avg(energy_kwh_out) as average_out,
    min(generationMissed_kwh) as minimum_missed,
    max(generationMissed_kwh) as maximum_missed,
    avg(generationMissed_kwh) as average_missed
FROM
    simulation
GROUP BY
    strategy,
    configuration,
    success
;

DROP TABLE IF EXISTS energy_scd_stat;

CREATE TABLE
    energy_scd_stat
AS
SELECT
    strategy,
    configuration,
    success,
    data,
    min(energy_kwh_in) as minimum_in,
    max(energy_kwh_in) as maximum_in,
    avg(energy_kwh_in) as average_in,
    min(energy_kwh_out) as minimum_out,
    max(energy_kwh_out) as maximum_out,
    avg(energy_kwh_out) as average_out,
    min(generationMissed_kwh) as minimum_missed,
    max(generationMissed_kwh) as maximum_missed,
    avg(generationMissed_kwh) as average_missed
FROM
    simulation
GROUP BY
    strategy,
    configuration,
    success,
    data
;

DROP TABLE IF EXISTS energy_scdt_stat;

CREATE TABLE
    energy_scdt_stat
AS
SELECT
    strategy,
    configuration,
    success,
    data,
    timeStep,
    min(energy_kwh_in) as minimum_in,
    max(energy_kwh_in) as maximum_in,
    avg(energy_kwh_in) as average_in,
    min(energy_kwh_out) as minimum_out,
    max(energy_kwh_out) as maximum_out,
    avg(energy_kwh_out) as average_out,
    min(generationMissed_kwh) as minimum_missed,
    max(generationMissed_kwh) as maximum_missed,
    avg(generationMissed_kwh) as average_missed
FROM
    simulation
GROUP BY
    strategy,
    configuration,
    success,
    data,
    timeStep
;