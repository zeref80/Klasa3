import matplotlib.pyplot as plt
import numpy as np


#Niestety, ani nie zrozumiałem w pełni jak użyć funkcji virtualnej tutaj, ani tym bardziej nie potrafiłem tego zrobić
#jednakże, żeby nie było, że nie zrobiłem zadania, to przesyłam coś takiego. Wykonuje to operacje do wielomianu 3 stopania ( w skrypcie co pan pokazywał na lekci było przedstawione do funkcji kwadratowej
# więc nie zastanawiałem się nad wyższymi wielomianami). Program normalnie wykonuje wykres podanej funkcji.
wyroznik1 = float(input("Wspolczynnik 1: "))
wyroznik2 = float(input("Wspolczynnik 2: "))
wyroznik3 = float(input("Wspolczynnik 3: "))
wyroznik4 = float(input("Wspolczynnik 4: "))
opisx = input("Wprowadź opis dla osi x")
opisy = input("Wprowadź opis dla osi y")
tytul = input("Wprowadź tytul wykresu")
wartoscx = np.linspace(-5, 5, 5000)
wartoscy = np.array([wyroznik1*(x**3)+wyroznik2*(x**2)+wyroznik3*(x)+ wyroznik4 for x in wartoscx])

fix, a = plt.subplots(figsize=(5,5))

a.plot(wartoscx, wartoscy)
plt.xlabel(opisx)
plt.ylabel(opisy)
plt.title(tytul)


plt.show()
