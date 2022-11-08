
internal_cartesian_execute <- function(
    vector_of_lists,
    execute_func,
    accumulator_func,
    accumulator,
    filter,
    index) {
    if (index > length(vector_of_lists)) {
        execute_func(accumulator)
    } else {
        if (filter(index)) {
            internal_cartesian_execute(
                vector_of_lists,
                execute_func,
                accumulator_func,
                accumulator,
                filter,
                index + 1)
        } else {
            for (l in vector_of_lists[[index]]) {
                internal_cartesian_execute(
                    vector_of_lists,
                    execute_func,
                    accumulator_func,
                    accumulator_func(l, accumulator, index),
                    filter,
                    index + 1)
            }
        }
    }
}

cartesian_execute <- function(
    vector_of_lists,
    execute_func,
    accumulator_func,
    initial_accumulator,
    filter) {
    internal_cartesian_execute(
        vector_of_lists,
        execute_func,
        accumulator_func,
        initial_accumulator,
        filter,
        1)
}