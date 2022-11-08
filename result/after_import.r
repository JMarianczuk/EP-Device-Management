library(RSQLite)

source("preprocessing/preprocessing.r")
source("r_helpers/sql_helpers.r")

con <- create_db_connection()
drop_preprocessed_tables(con)