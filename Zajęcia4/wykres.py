#Pytania i uwagi
#Mieliśmy zgłaszać ewentualne problemy więc u mnie jest to danie zmiennych w linspace i array
#O ile w .array mógło się dosyć mocno komplikować ze względu na budowe ( choć starałem się wymusić po prostu na użytkowniku
#by podał wzór dowolnej funkcji w postaci x*......)
#to w ogóle nie rozumiem, dlaczego wyrzuca mi błędy, kiedy chciałbym podstawić zmienne dla krańców dziedziny w .linspace.



import matplotlib.pyplot as plt
import numpy as np

#wzor = input("Wprowadź wzor: x*")
opisx = input("Wprowadź opis dla osi x")
opisy = input("Wprowadź opis dla osi y")
tytul = input("Wprowadź tytul wykresu")
#dziedzinad = input("Podaj dolny zakres dziedziny")
#dziedzinag = input("Podaj górny zakres dziedziny")
wartoscx = np.linspace(-10, 10, 5000)
wartoscy = np.array([x**3 for x in wartoscx])

fix, a = plt.subplots(figsize=(5,5))

a.plot(wartoscx, wartoscy)
plt.xlabel(opisx)
plt.ylabel(opisy)
plt.title(tytul)


plt.show()
