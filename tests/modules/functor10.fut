-- Three nested functors with the same named module type parameter.
-- ==
-- input { true } output { true }

module type mt = {
  type t
}

module f1(R: mt) = {
  type t = R.t
}

module f2(R: mt) = {
  module L = f1(R)
  open L
}

module f3(R: mt) = {
  module L = f2(R)
  open L
}


module m = f3({type t = bool})

fun main(x: m.t): m.t = x
