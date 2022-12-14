library(RSQLite)
library(ggplot2)
library(ggforce)
library(dplyr)
library(lubridate)
library(stringr)
library(scales)

Sys.setlocale(locale = "English")

source("r_helpers/all_helpers.r")

con <- create_db_connection("data.sqlite")

get_mid <- function(earlier_date, later_date, earlier_value, later_value) {
    #exactly one of earlier_value or later_value is negative, thus the minus operator
    fraction <- if (earlier_value - later_value == 0) 0.5 else earlier_value / (earlier_value - later_value)
    diff <- later_date - earlier_date
    fractioned_diff <- diff * fraction
    mid <- earlier_date + fractioned_diff
    mid
}

int_breaks <- function(x, n = 5) {
    breaks <- pretty(x, n)
    integer_breaks <- breaks[abs(breaks %% 1) < .Machine$double.eps ^ 0.5]
    integer_breaks
    # if (length(integer_breaks) <= 3) {
    #     breaks
    # } else {
    #     integer_breaks
    # }
}

load_name <- "Load [kW]"
generation_name <- "Generation [kW]"
net_load_name <- "Effective power [kW]"

for (data_set in c(
    "1LG"
    # , "2L"
    # , "3LG"
    # , "4LG"
    # , "4_Grid"
    # , "5L"
    # , "6LG"
    # , "6_Grid"
)) {
for (ts in c(
    # 60,
    240
    , 360
    , 1440
)) {
    table_name <- paste0("data_R", data_set, "_", ts, "min")
    query <- paste(
        # paste(
        #     "select",
        #     "*, load_kw as value,", quoted(load_name), "as facet",
        #     "from",
        #     table_name),
        # paste(
        #     "select",
        #     "*, generation_kw as value,", quoted(generation_name), "as facet",
        #     "from",
        #     table_name),
        paste(
            "select",
            "*, load_kw - generation_kw as value,", quoted(net_load_name), "as facet",
            "from",
            table_name),
        sep = " UNION " )

    res <- dbGetQuery(
        con,
        query)
    res$date <- ymd_hms(res$cet_cest_timestamp, tz = "CET", quiet = TRUE)
    res$color <- res$facet
    previous <- NULL
    extra_points <- NULL
    #extra_points <- data.frame(matrix(ncol = ncol(res), nrow = 0))
    # colnames(extra_points) <- colnames(res)
    extra_point_nextrow <- 2
    for (row in seq_len(nrow(res))) {
        entry <- res[row,]
        if (entry$facet == net_load_name) {
            current <- if (entry$value > 0) load_name else generation_name
            entry$color <- current
            res[row,]$color <- current
            if (!is.null(previous) && current != previous$color) {
                new_entry <- entry
                new_entry$date <- get_mid(previous$date, entry$date, previous$value, entry$value)
                new_entry$value <- 0
                if (is.null(extra_points)) {
                    extra_points <- data.frame(new_entry)
                } else {
                    extra_points[extra_point_nextrow,] <- new_entry
                    extra_point_nextrow <- extra_point_nextrow + 1
                }
            }
            previous <- entry
        }
    }
    res <- bind_rows(res, extra_points)
    factor_levels <- c(
            load_name,
            generation_name,
            net_load_name)
    res$facet_f <- factor(
        res$facet,
        levels = factor_levels)
    line_at_zero <- data.frame(
        value = 0,
        facet_f = factor(net_load_name, levels = factor_levels))
    thisplot <- ggplot(
        res,
        aes(
            x = date,
            y = value,
            color = color,
            group = facet_f,
        )) +
        geom_line() +
        geom_hline(data = line_at_zero, aes(yintercept = value), color = "black") +
        facet_grid(
            rows = vars(facet_f)
            , scales = "free_y"
            , switch = "both") +
        scale_x_datetime(
            expand = c(0.01, 0),
            # breaks = seq(
            #     from = ymd('2015-01-01', tz = "CET", quiet = TRUE),
            #     to = ymd('2018-01-01', tz = "CET", quiet = TRUE),
            #     by = "2 months"),
            date_labels = "%B %Y",
            date_minor_breaks = "1 month"
        ) +
        scale_y_continuous(
            breaks = int_breaks
            # , expand = c(0,0)
        ) +
        expand_limits(y = c(0, 2)) +
        labs(colour = "") +
        xlab("Date and Time") +
        theme(
            legend.position = "none",
            plot.margin = unit(c(0, 0, 0, 0), "cm"),
            axis.text.x = element_blank(),
            axis.ticks.x = element_blank(),
            axis.text.y = element_blank(),
            axis.ticks.y = element_blank(),
            axis.title.x = element_blank(),
            axis.title.y = element_blank())

    ggsave(paste0("dataplots/data_R", data_set, "_", ts, "min_eff.pdf"), thisplot, height = 4, width = 16.5, units = "cm")
}}

dbDisconnect(con)