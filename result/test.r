library(RSQLite)

source("r_helpers/sql_helpers.r")
source("r_helpers/string_helpers.r")
source("r_helpers/array_helpers.r")
source("r_helpers/ggplot_helpers.r")
source("r_helpers/colour_helpers.r")
source("r_helpers/functional_helpers.r")

source("preprocessing/successful_counts.r")
source("preprocessing/distinct_values.r")
source("preprocessing/energy_stat.r")
source("preprocessing/energy_counts.r")
source("preprocessing/energy_stat.r")

con <- create_db_connection()

# preprocess_successful_counts(c("strategy", "configuration", "data", "timeStep"), "scdt", "")

# preprocess_energy_counts("packetSize", "battery", con)


# preprocess_distinct(con)
# calculate_top_ten(con, c("strategy", "configuration", "data", "timeStep"), "scdt")
# cross_calculate_top_ten(con, c("strategy", "configuration", "data"), "scdt")
# preprocess_energy_stats(con)
# cross_calculate_top_ten_ordering(con, c("strategy", "configuration", "data"), "successful_counts_scd")
# drop_preprocessed_tables(con)
# drop_preprocessed_tables(con, "TableName like 'successful_counts%'")
# where <- get_where(strategy = "hi", seed = 1234, and = "data like you")
# print(where)

# df <- data.frame(
#     team=c('Mavs', 'Heat', 'Nets', 'Lakers', 'Probabilistic Range Control + Direction', 'Probabilistic Range Control + Direction + Estimation'),
#     points=c(24, 20, 34, 39, 28, 29),
#     assists=c(5, 7, 6, 9, 12, 13))

# colours <- qualitative_pallet(length(df$team))
# names(colours) <- df$team

# thisplot <- ggplot(df, aes(x = assists, y = points, colour = team)) +
#     geom_point(size = 3) +
#     theme(
#         # legend.spacing.y = unit(0.2, 'cm')
#         legend.key.size = unit(0.8, 'cm')
#         ) +
#     scale_colour_manual("team", values = colours, labels = label_wrap_gen(width = 29))
#     # guides(colour = guide_legend(byrow = TRUE))
# ggsave("test.pdf", thisplot)
format_percent(0.13456)
format_percent(0.42)
