library(ggplot2)

remove_legend <- function(plot) {
    plot + theme(legend.position = "none")
}