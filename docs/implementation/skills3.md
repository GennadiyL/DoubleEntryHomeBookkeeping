1. Empty blocks may place opening and closing braces on the same line (`{ }`). For nonempty blocks, put the opening brace, each statement, and the closing brace on separate lines.
2. Method and constructor signatures with up to two parameters may stay on one line. With more than two parameters, put each parameter on a separate line.
3. Object initializers with up to two member assignments may stay on one line. With more than two assignments, use a multiline layout: put the opening brace, each assignment, and the closing brace on separate lines.
4. Use explicit constructors declared inside class bodies. Do not use primary constructors. Target-typed `new()` is allowed.
5. In multiline conditions, put each operand at the same nesting level on its own line.
6. When wrapping expressions at an operator, keep the operator at the end of the preceding line.
7. Prefer overloads instead of optional parameters with default values. Allow exceptions for `CancellationToken cancellationToken = default`, caller-information parameters, and other justified exceptional cases.
