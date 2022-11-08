source("preprocessing/distinct_values.r")
source("preprocessing/energy_counts.r")
source("preprocessing/energy_stat.r")
source("preprocessing/successful_counts.r")

source("r_helpers/sql_helpers.r")

con <- create_db_connection()

preprocess_distinct(con)
preprocess_energy_stats(con)