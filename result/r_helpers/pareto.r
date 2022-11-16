

calculate_pareto_set <- function(set, compare_to) {
    result <- data.frame(set[1,])
    comparison_self <- compare_to(set[1,], set[1,])
    if (is.null(comparison_self) || length(comparison_self) == 0 || any(comparison_self != 0)) {
        stop("bad compare_to function")
    }
    for (set_index in seq_len(nrow(set)-1)+1) {
        dominates_entry_in_pareto_set <- FALSE
        dominated_by_entry_in_pareto_set <- FALSE
        set_element <- set[set_index,]
        for (result_index in seq_len(nrow(result))) {
            result_element <- result[result_index,]
            dominates_by_individual_values <- compare_to(set_element, result_element)
            if (any(dominates_by_individual_values > 0)) {
                dominates_entry_in_pareto_set <- TRUE
            }
            dominates_by_individual_values <- compare_to(result_element, set_element)
            if (any(dominates_by_individual_values > 0) && all(dominates_by_individual_values > -1)) {
                dominated_by_entry_in_pareto_set <- TRUE
                break
            }
        }
        add <- dominates_entry_in_pareto_set && !dominated_by_entry_in_pareto_set#!all(dominated_by_entry_in_pareto_set >= 0)
        if (add) {
            remove <- c()
            for (result_index in seq_len(nrow(result))) {
                result_element <- result[result_index,]
                dominates_by_individual_values <- compare_to(set_element, result_element)
                if (any(dominates_by_individual_values > 0) && all(dominates_by_individual_values > -1)) {
                    remove <- c(remove, result_index)
                }
            }
            if (!is.null(remove)) {
                result <- result[-remove,]
            }
            result <- rbind(result, set_element)
        }
    }
    result
}

compare_individual <- function(left, right, greater_value_dominates = TRUE) {
    res <- 0
    if (left > right) {
        res <- 1
    } else if (right > left) {
        res <- -1
    }
    if (!greater_value_dominates) {
        res <- -res
    }
    res
}