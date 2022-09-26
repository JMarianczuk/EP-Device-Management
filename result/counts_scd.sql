PRAGMA case_sensitive_like=ON;

DROP TABLE IF EXISTS successful_counts_scd;

CREATE TABLE
    successful_counts_scd
AS
SELECT
    strategy,
    configuration,
    success,
    data,
    count(*) as count
FROM
    simulation
GROUP BY
    strategy,
    configuration,
    success,
    data
;

DROP TABLE IF EXISTS simulation_with_counts_scd;

CREATE TABLE
    simulation_with_counts_scd
AS
SELECT
    s.*,
    c.count
FROM
    simulation s
JOIN
    successful_counts_scd c
ON
    s.strategy = c.strategy
    AND s.configuration = c.configuration
    AND s.success = c.success
    AND s.data = c.data
;