# UniLisp

Basic Lisp interpreter made to work in C# and created with a small Unity integration.

## Inspiration
UniLisp is heavily inspired by the 2 lispy articles by Peter Norvig:

- How to write a Lisp Interpreter in Python](http://norvig.com/lispy.html)
- An even better Lisp Interpreter in Python](https://norvig.com/lispy2.html)

## Features
UniLisp supports :

- Basic types: number (without boxing), string, symbol and List.
- Macro system
- An operator (#) to access any `public static` C# functions.
- Allows user to register C# function and C# Macros

UniLisp also has a `SearchProvider` that integrates with Unity Search Window and that act as a semi-REPL. The sample project contains a bunch of Unity Search Queries that encapsulate lisp snippets.