PRAGMA case_sensitive_like=ON;

DROP TABLE IF EXISTS energy_sc_stat;

CREATE TABLE
    energy_sc_stat
AS
SELECT
    strategy,
    configuration,
    success,
    min(energy_kwh) as minimum,
    max(energy_kwh) as maximum,
    avg(energy_kwh) as average
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
    min(energy_kwh) as minimum,
    max(energy_kwh) as maximum,
    avg(energy_kwh) as average
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
    min(energy_kwh) as minimum,
    max(energy_kwh) as maximum,
    avg(energy_kwh) as average
FROM
    simulation
GROUP BY
    strategy,
    configuration,
    success,
    data,
    timeStep
;