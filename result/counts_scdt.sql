PRAGMA case_sensitive_like=ON;

DROP TABLE IF EXISTS successful_counts_scdt;

CREATE TABLE
    successful_counts_scdt
AS
SELECT
    strategy,
    configuration,
	success,
    data,
    timeStep,
    count(*) as count
FROM
    simulation
GROUP BY
    strategy,
    configuration,
	success,
    data,
    timeStep
;

DROP TABLE IF EXISTS simulation_with_counts_scdt;

CREATE TABLE
    simulation_with_counts_scdt
AS
SELECT
    s.*,
    c.count
FROM
    simulation s
JOIN
    successful_counts_scdt c
ON
    s.strategy = c.strategy
    AND s.configuration = c.configuration
    AND s.success = c.success
    AND s.data = c.data
    AND s.timeStep = c.timeStep
;