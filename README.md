# GameOfLife
The Game Of Life ( https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life )

# Architectural decision log

## Planning phase, no code written yet, just a skeleton C# project.

- Assume API will be tested with very large grids, so come up with a strategy to handle this while being performant.  Don't assume any fixed grid size.
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

## Thinking more about the 'infinite' grid.

The cells must have coordinates, so I'm resigned that the grid cannot truly be infinite as far as having infinite coordinates.  And having infinite cells is not feasible due to RAM constraints.  So when we say that we are infinite, we must give the illusion of being infinite.  We could use 32-bit or 64-bit integers, I'm going to go with 64-bit under the assumption that the number of cells will be relatively small compared to how much RAM we have available for each coordinate to take up twice as much space.  This will allow the illusion of being infinite to persist much longer than a 32-bit coordinate would.

Next question is what to do when the coordinate overflows.  There's two ways to handle this:  either use unsigned 64-bit numbers which would make 0,0 the "upper-left" of the grid or use signed 64-bit numbers which would make 0,0 the center of the grid.  Using signed coordinates would be easier for a developer to parse when debugging, so that is a nice advantage.  Unsigned/signed integers both do the right thing when overflowing in this case, so we wouldn't necessarily lose any functionality with either choice.  Therefore, I'm settling on signed 64-bit ints as the better option for coordinates for the readability factor.

This type of design would mean that we would expect the grid would wraparound implicitly, so we'd need to make sure CheckForOverflowUnderflow is disabled.  If wraparound did occur, cells could collide with each other after wrapping and corrupt the results.  We should investigate writing a special unit test that covers this case, and make sure we are being intentional about the outcome rather than just leaving it as a big question mark.

Since we're using 64-bit integers, we can't actually allocate a 'very large grid' 

## Thinking about how to store board state internally and present board state via the API.

The requirements say to return next state or return X number of states away.  For the service layer, this sounds like the same method call (with 'next' being 1 state away).  We can implement two API calls.

It does not say what format the state needs to be returned in, nor does it say whether the state needs to be sorted.  This implies that we can use a Set or a Dictionary to store state internally, and return a list via the API to ensure that the JSON serializer gives the result we want.

The requirements say that the 'get final state' API fails after X attempts.  This implies that the API will also include a user configurable X value.  Do we want to adjust this value if the user passes in an obscenely huge one?  Since this is just a quick n' dirty project, we probably just let the user provide whatever they want and let the app run long.  If it were a large multi-user production service, we'd probably want to cap the X value so that one API call can't tie up a bunch of resources.

I am noticing the requirement to restart/crash/etc and that multiple boards can be created.  This implies that all of the API calls aside from create will take in an ID of an existing board.  We need to make sure we return 404 if they enter in an invalid ID and add a specific test for this.

## Fleshing out the interfaces

Since we're providing an alternate way for the API to create the board, I'm creating an IBoardServiceHelper interface to make unit tests easy to write.  I always try to avoid having a "unit" test that tests more than one method.  UPDATE: changed my mind on this one since I realized that the transformation method does not need to call the create method; the web layer can make one call to transform and one call to create.  This is cleaner and simpler than having a separate interface/class.

Since the request objects can be used by both the web and service layer, I'm moving them out of the web layer into the service layer.  UPDATE: changed my mind on this one since the way we're storing the coordinates internally is different from how we're returning it to the API caller.

## Board state

I'm thinking we should store both the initial and current state as well as the number of iterations that have elapsed.  While the current state could be inferred from the initial state and the iteration count, there is a performance cost to replay the iterations every time (and disk space is cheap) and even more importantly if we have the current state, we can dianose problems with our algorithm (or confirm that it's working correctly) which we would not be able to do otherwise.
