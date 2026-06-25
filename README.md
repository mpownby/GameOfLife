# GameOfLife
The Game Of Life ( https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life )

# Architectural decision log

## Planning phase, no code written yet, just a skeleton C# project.

- Assume API will be tested with very large grids, so come up with a stratagy to handle this while being performant.  Don't assume any fixed grid size.
- Optimize performance where possible.  This means detecting oscillators early and failing fast.  Specifically add some unit tests to ensure this behavior.
- Use Dependency Injection and interfaces for good encapsulation and testability.  Web layer depends on interface of service layer, service layer depends on interface of data layer.  Data layer implementation can just be a simple file on the disk but we'll design it abstract so it could be replaced with a database later if desired.
- Open questions:
    - How should the API look?  It wasn't specified in the requirements.  Since I'm assuming very large grids will be on the table, I'd better take in a list of X/Y coordinates to represent the grid.  So probably a JSON array of arrays.  I can optionally add another API call to take in a fixed array as a convenience.
    - Can I have a truly infinite grid?  Or am I constrained by the size of integers (32-bit or 64-bit)?
    - If I can't have a truly infinite grid, am I going to make the grid wraparound from 0 to INT_MAX (and vice versa) or just assume that cells on the borders are dead?  Wrapping around is probably better, I just need to make sure I have tests that handle this.
    - How am I going to update each cell simultaneously?  Do I keep two copies of the grid?
- Add Swashbuckle for human convenience.
- Test cases:
    - Test wraparound scenario using very large grid.
    - Manually write test for Block still life.  Get Claude to follow my pattern and write tests for the other still lifes.
    - Manually write test for Blinker oscillator.  Get Claude to follow my pattern and write tests for the other oscillators.
    - Same thing with Glider spaceship.
- Persistence: Do I need to persist a snapshot of the grid at the point of a crash or do I just need to persist the initial starting point and how iterations have elapsed?  It depends how cheap iterations are.
