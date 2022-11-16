library(RSQLite)
library(ggplot2)

qualitative_pallet <- function(n) {
    scale_fill_discrete()$palette(n)
    # viridis_pal(1, 0, 1, 1, "D")(n)
}

filter_legend <- function(colours, group, res) {
    result <- c()
    names <- c()
    old_colour_names <- names(colours)
    values <- unique(res[[group]])
    for (i in 1:length(colours)) {
        if (old_colour_names[i] %in% values) {
            result <- append(result, colours[i])
            names <- append(names, old_colour_names[i])
        }
    }
    result
}

get_group_colours <- function(group, res, con, transform = function(x) { x }) {
    group_values <- dbGetQuery(
        con,
        paste0(
            "select ",
            group,
            " from distinct_",
            group,
            " where success = ",
            '"True"'))[, 1]
    group_values_colours <- qualitative_pallet(length(group_values))
    names(group_values_colours) <- transform(group_values)
    group_values_colours <- filter_legend(group_values_colours, group, res)
    group_values_colours
}

get_group_linetypes <- function(group, res, con, transform = function(x) { x }) {
    group_values <- dbGetQuery(
        con,
        paste0(
            "select ",
            group,
            " from distinct_",
            group,
            " where success = ",
            '"True"'))[, 1]
    group_values_linetype <- scale_linetype_discrete()$palette(length(group_values))
    names(group_values_linetype) <- transform(group_values)
    group_values_linetype <- filter_legend(group_values_linetype, group, res)
    group_values_linetype
}