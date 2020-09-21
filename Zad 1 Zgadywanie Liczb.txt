# Z tego względu, że nie pamiętałem dokładnie treści zadania o jakim było wspomniane na ostatnich zdalnych e-lekcjach a klasa wiedziała,
# że chodzi głównie tylko o "zgadywanie liczb"  a nie jest to jakiś bardzo skomplikowany temat to nie zawracałem Panu głowy o szczegóły zadania
# (czy koputer pyta, czy użytkownik, przedzial liczb etc) tylko
# po prostu jakiś przykładowy text, który sprawdza wiedze i to zgadywanie:

print(" Pomysl o numerze od 1 do 150")
print("Moja pierwsza proba zgadniecia tego to:")
 
#wartosci:
tries = 0
max =150
min = 1
 
import random
guess = random.randint(min,max)
print(guess)
result = ""
 

while result != "dobrze":
   tries += 1
   result = input("Napisz czy 'nizej', 'wyzej' czy 'dobrze'")
   if result == "nizej":
       max = guess
       guess = random.randint(min,max)
       print(guess)
   elif result == "wyzej":
       min = guess
       guess = random.randint(min,max)
       print(guess)
   elif result == "dobrze":
       print(guess)
       print("Zgadnieto w ", tries, "probach!")