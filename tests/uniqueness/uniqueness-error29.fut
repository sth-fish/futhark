-- A local function whose free variable has been consumed.
-- ==
-- error: QUUX.*consumed

let main(y: i32, QUUX: *[]i32) =
  let f (x: i32) = x + QUUX[0]
  let QUUX[1] = 2
  in f y
