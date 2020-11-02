import pickle


class Student:
    def __init__(self, name, surname, index_number, rating_list=[]):
        self.__name = name
        self.__surname = surname
        self.__index_number = index_number
        self.__rating_list = rating_list[:]

    def show(self):
        print(f"Student: {self.__name} {self.__surname}")
        print(f"Index Number: {self.__index_number}")
        print(f"Rating list: {self.__rating_list}")
        print()

    def set_name(self, name):
        self.__name = name

    def set_surname(self, surname):
        self.__surname = surname

    def set_index_number(self, index_number):
        self.__index_number = index_number

    def set_rating_list(self, rating_list=[]):
        self.__rating_list = []
        self.__rating_list = rating_list[:]

    def get_name(self):
        return self.__name

    def get_surname(self):
        return self.__surname

    def get_index_number(self):
        return self.__index_number


def defense_int_input(text, min_val, max_val):
    value = input(text)
    while ((int(min_val) > int(value)) or (int(value) > int(max_val))):
        print('Wartosc musi byc nie mniejsza niz', min_val,
              'i nie wieksza niz', max_val, sep=' ', end='\n')
        value = input(text)

    return int(value)


def RepresentsInt(s):
    try:
        int(s)
        return True
    except ValueError:
        return False


def display_menu():
    menu_selection = 0
    print('\nMenu:')
    print('1 - Wyświetlanie listy studentów')
    print('2 - Edycja listy studentów')
    print('3 - Wyswietlanie wybranych studentów')
    print('4 - Odczytywanie listy z pliku')
    print('5 - Zapisywanie listy do pliku')
    print('6 - Koniec programu')

    print('\n')
    menu_selection = defense_int_input("Wybierz operacje: ", 1, 6)

    return menu_selection

def menu2():
    menu_selection = 0
    print('\nMenu edycji listy:')
    print('1 - Dodaj ucznia')
    print('2 - Usuń ucznia poprzez index tablicy')
    print('3 - Usuń ucznia poprzez index studencki')
    print('4 - Zmień imię ucznia w oparciu o index tablicy')
    print('5 - Zmień nazwisko ucznia w oparciu o index tablicy')
    print('6 - Zmień index ucznia w oparciu o index tablicy')
    print('7 - Zmień oceny ucznia w oparciu o index tablicy')
    print('8 - Powrót')

    print('\n')
    menu_selection = defense_int_input("Wybierz operacje: ", 1, 8)

    return menu_selection

def load():
    try:
        with open("bin.dat", "rb") as f:
            students_list = pickle.load(f)
            return students_list

    except Exception:
        pass


def save(students_list):
    with open("bin.dat", "wb") as f:
        pickle.dump(students_list, f)
		
		

def UsunStudentaPoprzezIndexTablicy():
    if len(students_list) > 0:
        i = int(input('Podaj index tablicy ucznia do usunięcia: '))
        students_list.remove(students_list[i-1])


def DodajStudenta():
    imie = input('Podaj imię: ')
    nazwisko = input('Podaj nazwisko: ')
    index_number = input('Podaj index studencki: ')

    oceny = []
    print("Wpisz liczby do tabeli (Pamiętaj - oddzielaj liczby spacją i nie wpisuj żadnego tekstu pomiędzy): ")
    oceny.extend(input().split(" "))

    count = 0
    for i in range(0, len(oceny)):
        if RepresentsInt(oceny[i]) == True:
            oceny[i - count] = int(oceny[i])
        else:
            count += 1
            continue

    if count > 0:
        lenght = len(oceny)
        for i in range(0, count):
            oceny.pop(lenght-1-i)

    newStudent = Student(imie, nazwisko, index_number, oceny)
    students_list.append(newStudent)

def DsunStudentaPoprzezIndex():
    if len(students_list) > 0:
        index = input('Podaj index studencki ucznia z listy do usunięcia: ')
        indexes = []
        for i in range(0, len(students_list)):
            if students_list[i].get_index_number() == int(index):
                indexes.append(i)

        if len(indexes) > 0:
            removed = 0
            for i in range(0, len(indexes)):
                students_list.pop(indexes[i] - removed)
                removed += 1


def ZmienImieStudenta():
    if len(students_list) > 0:
        id = int(input('Podaj index tablicy ucznia: '))
        name = input('Podaj nowe imię ucznia: ')
        students_list[id-1].set_name(name)


def ZmienNazwiskoStudenta():
    if len(students_list) > 0:
        id = int(input('Podaj index tablicy ucznia: '))
        surname = input('Podaj nowe nazwisko ucznia: ')
        students_list[id-1].set_surname(surname)


def ZmienIndexStudenta():
    if len(students_list) > 0:
        id = int(input('Podaj index tablicy ucznia: '))
        index = input('Podaj nowy index ucznia: ')
        students_list[id-1].set_index_number(index)


def ZmienOcenyStudenta():
    if len(students_list) > 0:
        id = int(input('Podaj index tablicy ucznia: '))
        oceny = []
        print("Wpisz liczby do tabeli (Pamiętaj - oddzielaj liczby spacją i nie wpisuj żadnego tekstu pomiędzy): ")
        oceny.extend(input().split(" "))

        count = 0
        for i in range(0, len(oceny)):
            if RepresentsInt(oceny[i]) == True:
                oceny[i - count] = int(oceny[i])
            else:
                count += 1
                continue

        if count > 0:
            lenght = len(oceny)
            for i in range(0, count):
                oceny.pop(lenght-1-i)

        students_list[id-1].set_rating_list(oceny)

def WyszukajUczniaPoIndexieTablicy():
    if len(students_list) > 0:
        id = int(input('Podaj index tablicy ucznia, którego chcesz wyświetlić: '))
        students_list[id-1].show()

print('Lista studentów.')
amount_of_students = 0
students_list = []

# student1 = Student("Rafał", "Nowak", 123456, [1, 2, 3, 4, 5])
# student2 = Student("Jan", "Kowalski", 234567, [2, 3, 4, 5])
# student3 = Student("Hanna", "Szymańska", 345678, [3, 4, 5])
# student4 = Student("Maja", "Jankowska", 456789, [4, 5])

# students_list.append(student1)
# students_list.append(student2)
# students_list.append(student3)
# students_list.append(student4)


# with open("bin.dat", "rb") as f:
#     s_l = pickle.load(f)
#     print(f"s_l: {s_l}")
#     for s in s_l:
#         s.show()

menu_selection = display_menu()


while(menu_selection < 6):
    if menu_selection == 1:
        print('\nLista studentów:\n')
        count = 1
        for student in students_list:
            print(f'id. {count}')
            student.show()
            print()
            count += 1

    elif menu_selection == 2:
        menu_selection = menu2()

        if menu_selection == 1:
            DodajStudenta()
        elif menu_selection == 2:
            UsunStudentaPoprzezIndexTablicy()
        elif menu_selection == 3:
            DsunStudentaPoprzezIndex()
        elif menu_selection == 4:
            ZmienImieStudenta()
        elif menu_selection == 5:
            ZmienNazwiskoStudenta()
        elif menu_selection == 6:
            ZmienIndexStudenta()
        elif menu_selection == 7:
            ZmienOcenyStudenta()

    elif menu_selection == 3:
        WyszukajUczniaPoIndexieTablicy()
    elif menu_selection == 4:
        students_list = load()
    elif menu_selection == 5:
        save(students_list)
        print('Zapisano do pliku.')

    menu_selection = display_menu()
