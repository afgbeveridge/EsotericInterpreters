{ Operator tests }
1 1=$["= Test 0 OK"]?~["= Test 0 failed"]?
1 2=$["= Test 1 failed"]?~["= Test 1 OK"]?
1 1>$["> Test 0 failed"]?~["> Test 0 OK"]?
2 1>$["> Test 1 OK"]?~["> Test 1 OK"]?
50a:a;0>a;99>~&$["~& Test 0 OK"]?~["~& Test 1 failed"]?
7 8 9 2( 
.",".",".",".
1 2 + 