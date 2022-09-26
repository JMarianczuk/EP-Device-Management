PRAGMA case_sensitive_like=ON;

DROP TABLE IF EXISTS successful_counts_sc;

CREATE TABLE
    successful_counts_sc
AS
SELECT
    strategy,
    configuration,
    success,
    count(*) as count
FROM
    simulation
GROUP BY
    strategy,
    configuration,
    success
;

DROP TABLE IF EXISTS simulation_with_counts_sc;

CREATE TABLE
    simulation_with_counts_sc
AS
SELECT
    s.*,
    c.count
FROM
    simulation s
JOIN
    successful_counts_sc c
ON
    s.strategy = c.strategy
    AND s.configuration = c.configuration
    AND s.success = c.success
;