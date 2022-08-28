# half-elmish-oo

This is experimental.
What happens if we mix mvu and mvvm together?

Important that this has no dependencies against any ui system.
I want to be able to use this in any dotnet app (that needs one or more states).
Wpf, winforms, avaloniaui, unity...

So yeah, so far it is experimental and i am just thinking out loud.
breaking changes might occur.

Gonna battle test this for a couple of weeks and then i might add documentation.
As it is now there is a bit of documentation in the code.

## heoo.lib

library project which contains the core elmish code.
This is what you should import into your own applications.

## heoo.example.code

library project which shows how an app can be built using heoo.lib.
This is what my theory how an app can be made without dependencies on a gui system
or dsl.
Which should make it portable.
Since elmishprogramasync is a class it can be instanced.
It should also be possible to break it into smaller parts and avoid the whole 
monolithic approach.

## heoo.example.wpf

This is a simple counter wpf application that shows how to databind ui to the code.
Should be a really thin layer coding wise.
