import statistics 

tabela = [1, 3, 5, 6, 3]
ans=True
while ans:
    print("""
    1.Wypisz tabele
    2.Pokaz srednia
    3.Pokaz mediane
    4.Pokaz najwieksza liczbe
    5.Pokaz najmniejsza liczbe
    6. Wyjscie z programu
    """)
    ans=input("Wybierz opcje? ")
    if ans=="1":
      for x in tabela:
        print(x)
    elif ans=="2":
      srednia = sum(tabela) / len(tabela)
      print("Srednia wynosi =", round(srednia, 2)) 
    elif ans=="3":
        res = statistics.median(tabela) 
        print("Mediana jest rowna : " + str(res)) 
    elif ans=="4":
      print("Maksymalny element : ", max(tabela))
    elif ans=="5":
      print("Maksymalny element : ", min(tabela))
    elif ans=="6":
      print("\n Bye") 
      ans = None
    else:
       print("\n Wpisz poprawna cyfre")