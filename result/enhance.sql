PRAGMA case_sensitive_like=ON;

DROP TABLE IF EXISTS successful_counts;

CREATE TABLE
    successful_counts
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

DROP VIEW IF EXISTS simulation_with_counts;

CREATE VIEW
    simulation_with_counts
AS
SELECT
    s.*,
    c.count
FROM
    simulation s
JOIN
    successful_counts c
ON
    s.strategy = c.strategy
    AND s.configuration = c.configuration
    AND s.success = c.success
    AND s.data = c.data
    AND s.timeStep = c.timeStep
;