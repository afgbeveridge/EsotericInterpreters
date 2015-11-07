{ Code from http://rosettacode.org/wiki/Category:FALSE) }

{ Perfect numbers }
[0\1[\$@$@-][\$@$@$@$@\/*=[@\$@+@@]?1+]#%=]p:
45p;!." "28p;!.   { 0 -1 }

{ General function }
12 7
\$@$@$@$@$@$@$@$@$@$@\  { 6 copies }
"
sum = "+."
difference = "-."
product = "*."
quotient = "/."
modulus = "/*-."
"
{ GCD }

[[$][$@$@$@\/*-]#%]g:
10 15 g;!"GCD 10 and 15 == ".  { 5 }

{ Ackermann function }
"
Ackermann: "
[$$[%
  \$$[%
     1-\$@@a;!  { i j -> A(i-1, A(i, j-1)) }
  1]?0=[
     %1         { i 0 -> A(i-1, 1) }
   ]?
  \1-a;!
1]?0=[
  %1+           { 0 j -> j+1 }
 ]?]a: { j i }
 
3 3 a;! .  { 61 }

)rs)
{ Ethopian multiplication }
)stEM)[2/]h:[2*]d:[1&]o:
[0[@$][$o;![@@\$@+@]?h;!@d;!@]#%\%]m:
"
Ethopian, 17 and 34: "
17 34m;!.{578}
"
")ct))ds)
{ Hailstone }
"

Hailstone start.....
")rs)
[$1&$[%3*1+0~]?~[2/]?]n:
[[$." "$1>][n;!]#%]s:
[1\[$1>][\1+\n;!]#%]c:
27s;! 27c;!"Hailstone sequence length of 27 is "."
")ds)"

Next phase; long running
"
99000s;! 99000c;!"Hailstone sequence length of 99000 is "."
Now execute for ages....

")rs)
)stHailstone < 1,000)
0m:0f:
1[$1000\>][$c;!$m;>[m:$f:0]?%1+]#%
f;." has hailstone sequence length "m;.
"
")ct)"
")ds)